namespace MazyPlatform.Service.Scenario.Repository.Api.Common.Extensions;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas.ValueObjects;

using DomainFieldType = MazyPlatform.Service.Scenario.Repository.Domain.Schemas.ValueObjects.FieldType;
using DomainPlatformType = MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects.PlatformType;
using ProtoFieldType = MazyPlatform.Contracts.Scenario.Repository.Grpc.FieldType;
using ProtoPlatformType = MazyPlatform.Contracts.Scenario.Repository.Grpc.PlatformType;

internal static class GrpcMappingExtensions
{
    private const ProtoPlatformType TelegramPlatformType = (ProtoPlatformType)3;

    public static DomainPlatformType ToDomain(this ProtoPlatformType platformType) => platformType switch
    {
        ProtoPlatformType.Universal => DomainPlatformType.Universal,
        ProtoPlatformType.Vk => DomainPlatformType.Vk,
        TelegramPlatformType => DomainPlatformType.Telegram,
        ProtoPlatformType.Unspecified => throw new RpcException(new Status(StatusCode.InvalidArgument, "Тип платформы не может быть Unspecified.")),
        _ => throw new ArgumentException($"Неподдерживаемый тип платформы: {platformType}", nameof(platformType)),
    };

    public static DomainFieldType ToDomain(this ProtoFieldType fieldType) => fieldType switch
    {
        ProtoFieldType.String => DomainFieldType.String,
        ProtoFieldType.Number => DomainFieldType.Number,
        ProtoFieldType.Boolean => DomainFieldType.Boolean,
        ProtoFieldType.DateTime => DomainFieldType.DateTime,
        ProtoFieldType.Reference => DomainFieldType.Reference,
        ProtoFieldType.Enum => DomainFieldType.Enum,
        ProtoFieldType.Unspecified => throw new RpcException(new Status(StatusCode.InvalidArgument, "Тип поля не может быть Unspecified.")),
        _ => throw new ArgumentException($"Неподдерживаемый тип поля: {fieldType}", nameof(fieldType)),
    };

    public static ProtoFieldType ToProto(this DomainFieldType fieldType) => fieldType switch
    {
        DomainFieldType.String => ProtoFieldType.String,
        DomainFieldType.Number => ProtoFieldType.Number,
        DomainFieldType.Boolean => ProtoFieldType.Boolean,
        DomainFieldType.DateTime => ProtoFieldType.DateTime,
        DomainFieldType.Reference => ProtoFieldType.Reference,
        DomainFieldType.Enum => ProtoFieldType.Enum,
        _ => throw new ArgumentException($"Неподдерживаемый тип поля: {fieldType}", nameof(fieldType)),
    };

    public static ProtoPlatformType ToProto(this DomainPlatformType platformType) => platformType switch
    {
        DomainPlatformType.Vk => ProtoPlatformType.Vk,
        DomainPlatformType.Telegram => TelegramPlatformType,
        DomainPlatformType.Universal => ProtoPlatformType.Universal,
        _ => throw new ArgumentException($"Неподдерживаемый тип платформы: {platformType}", nameof(platformType)),
    };
}
