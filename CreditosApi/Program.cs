using Confluent.Kafka;
using CreditosApi.Data;
using CreditosApi.Data.Repositories;
using CreditosApi.Interfaces;
using CreditosApi.Models.Queries;
using CreditosApi.Models.Response;
using CreditosApi.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configurar Serilog com mensagens formatadas
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3-u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicação CreditosApi");

    var builder = WebApplication.CreateBuilder(args);

    // Configurar Serilog para usar configuração do appsettings.json
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3-u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3-u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddScoped<IApplicationBus, ApplicationBus>();
    builder.Services.AddScoped<IQueryHandler<CreditoConstituidoConsultaQuery, CreditoIntegracaoResponse?>, CreditoQueryHandler>();

    // Prioridade: Variável de ambiente > appsettings.json > default
    // No Docker, usar 'db' como host. Localmente, usar 'localhost'
    var dbHost = builder.Configuration["DB_HOST"] ?? "localhost";
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? $"Host={dbHost};Port=5432;Database=CreditosDb;Username=postgres;Password=senha123";
    
    // Log da string de conexão (sem a senha por segurança)
    var loggedConnectionString = connectionString.Replace("Password=senha123", "Password=***");
    Log.Information("String de conexão configurada: {ConnectionString}", loggedConnectionString);

    builder.Services.AddDbContext<CreditoDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Configuração do Kafka Consumer
    // Prioridade: Variável de ambiente > appsettings.json > default
    var kafkaBootstrapServers = builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"]
        ?? builder.Configuration["Kafka:BootstrapServers"]
        ?? "localhost:9092";

    builder.Services.AddSingleton<IConsumer<Ignore, string>>(sp =>
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "creditos-api-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        return new ConsumerBuilder<Ignore, string>(config).Build();
    });

    // Configuração do Kafka Producer
    builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

    // Registro dos serviços
    builder.Services.AddScoped<ICreditoRepository, CreditoRepository>();

    // Registro do Kafka Message Handler (implementação básica)
    builder.Services.AddScoped<IKafkaMessageHandler, KafkaMessageHandler>();

    // Registro do processador de créditos
    builder.Services.AddScoped<ICreditoProcessor, CreditoProcessor>();

    // Registro do Background Service do Kafka
    builder.Services.AddHostedService<KafkaBackgroundService>();

    // Registro do Job do Quartz (deve ser registrado como tipo concreto para o ServiceProviderJobFactory)
    builder.Services.AddScoped<CreditoProcessorJob>();

    // Registro do Background Service para processamento de créditos com Quartz
    // Este serviço gerencia o scheduler do Quartz e agenda o job
    builder.Services.AddHostedService<CreditoProcessorBackgroundService>();


    var app = builder.Build();

    // Executar migrations em Development ou Container
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Container")
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CreditoDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Verificando conexão com o banco de dados...");
            logger.LogInformation("Ambiente atual: {Environment}", app.Environment.EnvironmentName);
            logger.LogInformation("String de conexão: {ConnectionString}", loggedConnectionString);

            // Aguardar até o banco estar disponível (até 60 segundos)
            var maxRetries = 60;
            var retryCount = 0;
            var connected = false;

            while (retryCount < maxRetries && !connected)
            {
                try
                {
                    logger.LogInformation("Tentando conectar ao banco de dados... Tentativa {RetryCount}/{MaxRetries}", retryCount + 1, maxRetries);
                    connected = await dbContext.CanConnectAsync();
                    if (!connected)
                    {
                        retryCount++;
                        logger.LogWarning("Banco de dados não disponível. Tentativa {RetryCount}/{MaxRetries}...", retryCount, maxRetries);
                        await Task.Delay(2000); // Aumentar o tempo de espera entre tentativas
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    logger.LogWarning(ex, "Erro ao conectar ao banco. Tentativa {RetryCount}/{MaxRetries}...", retryCount, maxRetries);
                    await Task.Delay(2000); // Aumentar o tempo de espera entre tentativas
                }
            }

            if (connected)
            {
                logger.LogInformation("Conexão com o banco de dados estabelecida com sucesso!");
                logger.LogInformation("Aplicando migrações de banco de dados...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrações de banco de dados aplicadas com sucesso");
            }
            else
            {
                logger.LogError("Não foi possível conectar ao banco de dados após {MaxRetries} tentativas", maxRetries);
                // Tentar mostrar detalhes adicionais do erro
                try
                {
                    logger.LogInformation("Tentando obter informações adicionais sobre a conexão...");
                    var connection = dbContext.Database.GetDbConnection();
                    logger.LogInformation("Connection String: {ConnectionString}", connection.ConnectionString);
                    logger.LogInformation("Connection State: {ConnectionState}", connection.State);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao obter informações adicionais da conexão");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aplicar migrações de banco de dados");
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Container")
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Verificar se HTTPS está habilitado através de variáveis de ambiente
    var useHttps = builder.Configuration.GetValue<bool>("USE_HTTPS", false);
    
    if (useHttps)
    {
        app.UseHttpsRedirection();
    }
    
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrada inesperadamente");
    throw;
}
finally
{
    Log.CloseAndFlush();
}