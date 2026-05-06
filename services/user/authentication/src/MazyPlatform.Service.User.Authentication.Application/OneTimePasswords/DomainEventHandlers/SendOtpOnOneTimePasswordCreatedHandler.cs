namespace MazyPlatform.Service.User.Authentication.Application.OneTimePasswords.DomainEventHandlers;

using MazyPlatform.Contracts.User.Authentication.Events;
using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Events;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.SharedKernel.Application.Abstractions.Events;

using Microsoft.Extensions.Logging;

internal sealed partial class SendOtpOnOneTimePasswordCreatedHandler(
    IUserAccountRepository accountRepository,
    IIntegrationEventPublisher eventPublisher,
    ILogger<SendOtpOnOneTimePasswordCreatedHandler> logger) : IDomainEventHandler<OneTimePasswordCreatedDomainEvent>
{
    public async Task HandleAsync(OneTimePasswordCreatedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var task = @event.Type switch
        {
            OtpType.EmailConfirmation => PublishConfirmationCodeAsync(@event, cancellationToken),
            OtpType.MfaEmail => PublishMfaEmailCodeAsync(@event, cancellationToken),
            OtpType.PasswordReset => PublishPasswordResetCodeAsync(@event, cancellationToken),
            _ => throw new NotSupportedException($"Тип OTP {@event.Type} не поддерживается для отправки кода подтверждения."),
        };

        await task;
    }

    private async Task PublishConfirmationCodeAsync(OneTimePasswordCreatedDomainEvent @event, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(@event.UserAccountId, cancellationToken);
        if (account is null)
        {
            UserAccountNotFound(@event.UserAccountId);
            return;
        }

        var integrationEvent = new UserAccountRegisteredIntegrationEvent(@event.OccurredAt, @event.UserAccountId, account.Email.Value, @event.Code);

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);
        ConfirmationCodePublished(@event.UserAccountId, account.Email.Value);
    }

    private async Task PublishMfaEmailCodeAsync(OneTimePasswordCreatedDomainEvent @event, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(@event.UserAccountId, cancellationToken);
        if (account is null)
        {
            UserAccountNotFound(@event.UserAccountId);
            return;
        }

        var integrationEvent = new UserAccountMfaEmailCodeGeneratedIntegrationEvent(@event.OccurredAt, @event.UserAccountId, account.Email.Value, @event.Code);
        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);
        MfaEmailCodePublished(@event.UserAccountId, account.Email.Value);
    }

    private async Task PublishPasswordResetCodeAsync(OneTimePasswordCreatedDomainEvent @event, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(@event.UserAccountId, cancellationToken);
        if (account is null)
        {
            UserAccountNotFound(@event.UserAccountId);
            return;
        }

        var integrationEvent = new UserAccountPasswordResetRequestedIntegrationEvent(@event.OccurredAt, @event.UserAccountId, account.Email.Value, @event.Code);
        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);
        PasswordResetCodePublished(@event.UserAccountId, account.Email.Value);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Error, "Не найден агрегат UserAccount с ID '{UserAccountId}' при отправке кода подтверждения.")]
    private partial void UserAccountNotFound(Guid userAccountId);

    [LoggerMessage(2, LogLevel.Information, "Код подтверждения для UserAccount {UserAccountId} с email {Email} успешно опубликован.")]
    private partial void ConfirmationCodePublished(Guid userAccountId, string email);

    [LoggerMessage(3, LogLevel.Information, "MFA email-код для UserAccount {UserAccountId} с email {Email} успешно опубликован.")]
    private partial void MfaEmailCodePublished(Guid userAccountId, string email);

    [LoggerMessage(4, LogLevel.Information, "Код сброса пароля для UserAccount {UserAccountId} с email {Email} успешно опубликован.")]
    private partial void PasswordResetCodePublished(Guid userAccountId, string email);
    #endregion
}
