namespace MazyPlatform.Service.Notification.IntegrationEventHandlers;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.User.Authentication.Events;
using MazyPlatform.Service.Notification.Messaging.Abstractions;

internal sealed class UserAccountMfaEmailCodeGeneratedIntegrationEventHandler(IEmailSender emailSender) : IIntegrationEventHandler<UserAccountMfaEmailCodeGeneratedIntegrationEvent>
{
    private readonly IEmailSender _emailSender = emailSender;

    public async Task HandleAsync(UserAccountMfaEmailCodeGeneratedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        await _emailSender.SendAsync(
            to: @event.Email,
            subject: "Код подтверждения MFA",
            body: EmailBodyTemplates.BuildCodeEmail(
                title: "Подтверждение входа",
                description: "Используйте код ниже для подтверждения входа в аккаунт.",
                code: @event.Code,
                additionalInfo: "Если вы не запрашивали код, просто проигнорируйте это письмо."),
            cancellationToken: cancellationToken);
    }
}
