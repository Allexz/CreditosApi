using CreditosApi.Data;
using CreditosApi.Data.Repositories;
using CreditosApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicação CreditosApi");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host
        .UseSerilog((context, configuration) =>
        configuration
        .ReadFrom.Configuration(context.Configuration));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=db;Port=5432;Database=CreditosDb;Username=postgres;Password=senha123";

    builder.Services.AddDbContext<CreditoDbContext>(options =>
        options.UseNpgsql(connectionString));
    
    builder.Services.AddScoped<ICreditoRepository, CreditoRepository>();

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

            // Aguardar até o banco estar disponível (até 30 segundos)
            var maxRetries = 30;
            var retryCount = 0;
            var connected = false;

            while (retryCount < maxRetries && !connected)
            {
                try
                {
                    connected = await dbContext.CanConnectAsync();
                    if (!connected)
                    {
                        retryCount++;
                        logger.LogWarning("Banco de dados não disponível. Tentativa {RetryCount}/{MaxRetries}...", retryCount, maxRetries);
                        await Task.Delay(1000);
                    }
                }
                catch
                {
                    retryCount++;
                    logger.LogWarning("Erro ao conectar ao banco. Tentativa {RetryCount}/{MaxRetries}...", retryCount, maxRetries);
                    await Task.Delay(1000);
                }
            }

            if (connected)
            {
                logger.LogInformation("Aplicando migrações de banco de dados...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrações de banco de dados aplicadas com sucesso");
            }
            else
            {
                logger.LogError("Não foi possível conectar ao banco de dados após {MaxRetries} tentativas", maxRetries);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aplicar migrações de banco de dados");
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
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

