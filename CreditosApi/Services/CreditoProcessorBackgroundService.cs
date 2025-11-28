using Confluent.Kafka;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace CreditosApi.Services;

/// <summary>
/// BackgroundService que configura e gerencia o Quartz para processar mensagens do Kafka
/// </summary>
public class CreditoProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CreditoProcessorBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private IScheduler? _scheduler;
    private IConsumer<Ignore, string>? _consumer;
    private const string TOPIC_NAME = "integrar-credito-constituido-entry";

    public CreditoProcessorBackgroundService(IServiceProvider serviceProvider,
                                             ILogger<CreditoProcessorBackgroundService> logger,
                                             IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Iniciando CreditoProcessorBackgroundService...");

            // Configura o scheduler do Quartz
            var factory = new StdSchedulerFactory();
            _scheduler = await factory.GetScheduler(stoppingToken);

            // Configura o job factory para usar o service scope factory
            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            _scheduler.JobFactory = new ServiceProviderJobFactory(scopeFactory);

            // Inicia o scheduler
            await _scheduler.Start(stoppingToken);

            _logger.LogInformation("Scheduler do Quartz iniciado");

            // Cria e subscreve ao Consumer
            var kafkaBootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"]
                ?? _configuration["Kafka:BootstrapServers"]
                ?? "localhost:9092";

            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaBootstrapServers,
                GroupId = "credito-processor-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            
            // Aguarda um pouco para garantir que o tópico foi criado pelo kafka-init
            await Task.Delay(5000, stoppingToken);
            
            // Se inscreve no tópico (o tópico deve ter sido criado pelo kafka-init)
            _consumer.Subscribe(TOPIC_NAME);
            _logger.LogInformation("Subscrito ao tópico {Topic}", TOPIC_NAME);

            // Passa o Consumer para o Job através do JobDataMap
            var jobDataMap = new JobDataMap
            {
                ["Consumer"] = _consumer
            };

            // Cria e agenda o job para executar a cada 500ms
            var job = JobBuilder.Create<CreditoProcessorJob>()
                .WithIdentity("credito-processor-job", "kafka-group")
                .UsingJobData(jobDataMap)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("credito-processor-trigger", "kafka-group")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();

            // Agenda o job
            await _scheduler.ScheduleJob(job, trigger, stoppingToken);

            _logger.LogInformation(
                "Job de processamento de créditos agendado para executar a cada 500ms no tópico {Topic}",
                TOPIC_NAME);

            // Mantém o serviço rodando
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar CreditoProcessorBackgroundService");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parando CreditoProcessorBackgroundService...");

        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            _logger.LogInformation("Scheduler do Quartz parado");
        }

        if (_consumer != null)
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("Consumer do Kafka fechado");
        }

        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// JobFactory customizado que usa o ServiceScopeFactory para criar jobs com escopo
/// </summary>
public class ServiceProviderJobFactory : IJobFactory
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ServiceProviderJobFactory(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobType = bundle.JobDetail.JobType;
        
        // Cria um scope para resolver serviços Scoped
        var scope = _serviceScopeFactory.CreateScope();
        var job = (IJob)scope.ServiceProvider.GetRequiredService(jobType);
        
        // Cria um wrapper que gerencia o scope
        return new ScopedJobWrapper(job, scope);
    }

    public void ReturnJob(IJob job)
    {
        // Descarta o scope quando o job terminar
        if (job is ScopedJobWrapper wrapper)
        {
            wrapper.Dispose();
        }
    }

    /// <summary>
    /// Wrapper que gerencia o ciclo de vida do scope para o job
    /// </summary>
    private class ScopedJobWrapper : IJob, IDisposable
    {
        private readonly IJob _job;
        private readonly IServiceScope _scope;

        public ScopedJobWrapper(IJob job, IServiceScope scope)
        {
            _job = job ?? throw new ArgumentNullException(nameof(job));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _job.Execute(context);
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}

