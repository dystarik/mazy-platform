namespace MazyPlatform.Service.Notification.IntegrationEventHandlers;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.User.Authentication.Events;
using MazyPlatform.Service.Notification.Messaging.Abstractions;

internal sealed class UserAccountRegisteredIntegrationEventHandler(IEmailSender emailSender) : IIntegrationEventHandler<UserAccountRegisteredIntegrationEvent>
{
    private readonly IEmailSender _emailSender = emailSender;

    public async Task HandleAsync(UserAccountRegisteredIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        await _emailSender.SendAsync(
            to: @event.Email,
            subject: "Подтверждение регистрации",
            body: EmailBodyTemplates.BuildCodeEmail(
                title: "Подтверждение регистрации",
                description: "Спасибо за регистрацию. Введите код ниже, чтобы подтвердить ваш email.",
                code: @event.ConfirmationCode,
                additionalInfo: "Если вы не создавали аккаунт, просто проигнорируйте это письмо."),
            cancellationToken: cancellationToken);
    }
}
