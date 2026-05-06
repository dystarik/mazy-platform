namespace MazyPlatform.Service.Notification.IntegrationEventHandlers;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.User.Authentication.Events;
using MazyPlatform.Service.Notification.Messaging.Abstractions;

internal sealed class UserAccountPasswordResetRequestedIntegrationEventHandler(IEmailSender emailSender) : IIntegrationEventHandler<UserAccountPasswordResetRequestedIntegrationEvent>
{
    public async Task HandleAsync(UserAccountPasswordResetRequestedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        await emailSender.SendAsync(
            to: @event.Email,
            subject: "Сброс пароля",
            body: EmailBodyTemplates.BuildCodeEmail(
                title: "Сброс пароля",
                description: "Мы получили запрос на сброс пароля. Используйте код ниже для продолжения.",
                code: @event.ResetCode,
                additionalInfo: "Если вы не запрашивали сброс, рекомендуем проверить безопасность аккаунта."),
            cancellationToken: cancellationToken);
    }
}
