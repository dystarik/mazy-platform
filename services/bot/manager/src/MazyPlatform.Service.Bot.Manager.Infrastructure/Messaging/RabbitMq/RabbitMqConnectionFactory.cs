namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Messaging.RabbitMq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

internal sealed partial class RabbitMqConnectionFactory(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionFactory> logger) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            Connected(_options.Host, _options.Port);
            return _connection;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _connection?.Dispose();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Подключение к RabbitMQ установлено. Host: {Host}, Port: {Port}.")]
    private partial void Connected(string host, int port);
    #endregion
}
