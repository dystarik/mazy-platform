using MazyPlatform.Service.Bot.Integration.Configuration;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Инициализация сервиса");
    var host = Host
        .CreateApplicationBuilder(args)
        .AddConfigure()
        .Build();

    Log.Information("Сервис готов");
    await host.RunAsync();
}
catch (OperationCanceledException)
{
    Log.Information("Сервис завершил работу по запросу");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Сервис завершил работу с ошибкой");
}
finally
{
    await Log.CloseAndFlushAsync();
}
