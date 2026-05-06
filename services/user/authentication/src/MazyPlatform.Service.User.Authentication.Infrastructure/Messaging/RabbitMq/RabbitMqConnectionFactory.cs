namespace MazyPlatform.Service.User.Authentication.Infrastructure.Messaging.RabbitMq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

/// <summary>
/// Потокобезопасная фабрика RabbitMQ-соединений с ленивой инициализацией и повторным использованием открытого соединения.
/// </summary>
/// <remarks>
/// Использует <see cref="SemaphoreSlim"/> для исключения гонки при одновременном первом обращении из нескольких потоков
/// (double-checked locking). Если соединение уже открыто (<see cref="IConnection.IsOpen"/>),
/// повторное подключение не выполняется.
/// </remarks>
internal sealed partial class RabbitMqConnectionFactory(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionFactory> logger) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly RabbitMqOptions _options = options.Value;
    private readonly ILogger<RabbitMqConnectionFactory> _logger = logger;
    private IConnection? _connection;

    /// <summary>
    /// Возвращает открытое соединение с RabbitMQ, создавая его при первом обращении.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции подключения.</param>
    /// <returns>Открытое <see cref="IConnection"/> с брокером.</returns>
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

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore.Dispose();
        _connection?.Dispose();
    }

    [LoggerMessage(1, LogLevel.Information, "Подключение к RabbitMQ установлено. Host: {Host}, Port: {Port}.")]
    private partial void Connected(string host, int port);
}
