namespace MazyPlatform.Service.User.Authentication.Api.Common.Extensions;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;
using MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using DomainBackupCode = MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.BackupCode;
using DomainExternalProvider = MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders.ExternalProviderType;
using DomainMfaMethodType = MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.MfaMethodType;
using DomainMfaSessionAction = MazyPlatform.Service.User.Authentication.Domain.MfaSessions.MfaSessionAction;
using ProtoExternalProvider = MazyPlatform.Contracts.User.Grpc.Authentication.ExternalProvider;
using ProtoMfaFactorType = MazyPlatform.Contracts.User.Grpc.Authentication.MfaFactorType;
using ProtoMfaSessionAction = MazyPlatform.Contracts.User.Grpc.Authentication.MfaSessionAction;

internal static class GrpcMappingExtensions
{
    public static DomainMfaSessionAction ToDomain(this ProtoMfaSessionAction action) => action switch
    {
        ProtoMfaSessionAction.Login => DomainMfaSessionAction.Login,
        ProtoMfaSessionAction.RegenerateBackupCodes => DomainMfaSessionAction.GenerateBackupCodes,
        ProtoMfaSessionAction.RemoveFactor => DomainMfaSessionAction.DeleteMfaMethod,
        ProtoMfaSessionAction.ChangePassword => DomainMfaSessionAction.ChangePassword,
        ProtoMfaSessionAction.Unspecified => throw new RpcException(new Status(StatusCode.InvalidArgument, "Действие не может быть Unspecified.")),
        _ => throw new ArgumentException($"Неподдерживаемое действие MFA: {action}", nameof(action)),
    };

    public static DomainMfaMethodType ToDomain(this ProtoMfaFactorType type) => type switch
    {
        ProtoMfaFactorType.Totp => DomainMfaMethodType.Totp,
        ProtoMfaFactorType.Email => DomainMfaMethodType.Email,
        ProtoMfaFactorType.BackupCode => DomainMfaMethodType.BackupCode,
        ProtoMfaFactorType.Unspecified => throw new RpcException(new Status(StatusCode.InvalidArgument, "Тип MFA метода не может быть Unspecified.")),
        _ => throw new ArgumentException($"Неподдерживаемый MFA фактор: {type}", nameof(type)),
    };

    public static DomainExternalProvider ToDomain(this ProtoExternalProvider type) => type switch
    {
        ProtoExternalProvider.Yandex => DomainExternalProvider.Yandex,
        ProtoExternalProvider.Unspecified => throw new RpcException(new Status(StatusCode.InvalidArgument, "Тип внешнего провайдера не может быть Unspecified.")),
        _ => throw new ArgumentException($"Неподдерживаемый внешний провайдер: {type}", nameof(type)),
    };

    public static ProtoMfaFactorType ToProto(this DomainMfaMethodType type) => type switch
    {
        DomainMfaMethodType.Totp => ProtoMfaFactorType.Totp,
        DomainMfaMethodType.Email => ProtoMfaFactorType.Email,
        DomainMfaMethodType.BackupCode => ProtoMfaFactorType.BackupCode,
        _ => throw new ArgumentException($"Неподдерживаемый тип MFA фактора: {type}", nameof(type)),
    };

    public static ProtoExternalProvider ToProto(this DomainExternalProvider type) => type switch
    {
        DomainExternalProvider.Yandex => ProtoExternalProvider.Yandex,
        _ => throw new ArgumentException($"Неподдерживаемый внешний провайдер: {type}", nameof(type)),
    };

    public static IEnumerable<ProtoMfaFactorType> ToProto(this IEnumerable<DomainMfaMethodType> factors) =>
        factors.Select(f => f.ToProto());

    public static MfaChallenge ToProto(this LoginByPasswordResult.MfaRequired data) => new()
    {
        MfaSessionId = data.MfaSessionId.ToString(),
        RequiredFactorCount = data.RequiredFactorCount,
        AvailableFactors = { data.AvailableFactors.ToProto() },
    };

    public static MfaChallenge ToProto(this ChangePasswordResult.MfaRequired data) => new()
    {
        MfaSessionId = data.MfaSessionId.ToString(),
        RequiredFactorCount = data.RequiredFactorCount,
        AvailableFactors = { data.AvailableFactors.ToProto() },
    };

    public static MfaChallenge ToProto(this ResetPasswordResult.MfaChallenge data) => new()
    {
        MfaSessionId = data.MfaSessionId.ToString(),
        RequiredFactorCount = data.RequiredFactorCount,
        AvailableFactors = { data.AvailableFactors.ToProto() },
    };

    public static OtpChallenge ToProto(this ResetPasswordResult.OtpChallenge data) => new()
    {
        OtpId = data.OtpId.ToString(),
    };

    public static TokenPair ToProto(this LoginByPasswordResult.Success data) => new()
    {
        AccessToken = data.AccessToken,
        RefreshToken = data.RefreshToken,
    };

    public static TokenPair ToProto(this CompleteRegistrationResult data) => new()
    {
        AccessToken = data.AccessToken,
        RefreshToken = data.RefreshToken,
    };

    public static TokenPair ToProto(this RefreshSessionResult data) => new()
    {
        AccessToken = data.AccessToken,
        RefreshToken = data.RefreshToken,
    };

    public static IEnumerable<string> ToProto(this IEnumerable<DomainBackupCode> backupCodes) => backupCodes.Select(x => x.Code);
}
