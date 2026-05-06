namespace MazyPlatform.Service.Notification.Messaging;

using MailKit.Net.Smtp;

using MazyPlatform.Service.Notification.Configuration.Options;
using MazyPlatform.Service.Notification.Messaging.Abstractions;

using Microsoft.Extensions.Options;

using MimeKit;

internal sealed partial class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.DisplayName, _options.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();

        LogConnecting(_options.Host, _options.Port);

        await client.ConnectAsync(_options.Host, _options.Port, true, cancellationToken);
        await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        LogSent(to, subject);
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Подключение к SMTP. Host: {Host}, Port: {Port}.")]
    private partial void LogConnecting(string host, int port);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Письмо отправлено. To: {To}, Subject: {Subject}.")]
    private partial void LogSent(string to, string subject);
    #endregion
}
