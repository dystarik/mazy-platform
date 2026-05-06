using MazyPlatform.Service.User.Authentication.Api.Configuration;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Инициализация сервиса");
    var app = WebApplication.CreateBuilder()
        .AddConfigure()
        .Build()
        .UseMiddleware();

    Log.Information("Сервис готов");
    await app.RunAsync();
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
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}
