namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations.UserAccounts;

using System.Text.Json;
using System.Text.Json.Serialization;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.Service.User.Authentication.Infrastructure.Security.Totp;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MfaMethodConfiguration(ITotpSecretEncryptor totpEncryptor) : IEntityTypeConfiguration<MfaMethod>
{
    private readonly JsonSerializerOptions _payloadJsonOptions = new()
    {
        Converters = { new MfaPayloadJsonConverter(totpEncryptor), },
    };

    public void Configure(EntityTypeBuilder<MfaMethod> builder)
    {
        builder.ToTable("mfa_methods");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        builder.Property(x => x.MfaSettingsId)
            .HasColumnName("mfa_settings_id")
            .IsRequired();

        builder
            .HasOne<MfaSettings>()
            .WithMany(x => x.MfaMethods)
            .HasForeignKey(x => x.MfaSettingsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Payload)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _payloadJsonOptions),
                v => JsonSerializer.Deserialize<IMfaPayload>(v, _payloadJsonOptions)!)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .IsRequired(false);

        builder.Ignore(x => x.Type);
        builder.Ignore(x => x.IsConfirmed);
    }

    private sealed class MfaPayloadJsonConverter(ITotpSecretEncryptor encryptor) : JsonConverter<IMfaPayload>
    {
        public override IMfaPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var type = (MfaMethodType)root.GetProperty("type").GetInt32();

            return type switch
            {
                MfaMethodType.Totp => new TotpPayload(encryptor.Decrypt(root.GetProperty("secret").GetString()!)),
                MfaMethodType.Email => new EmailPayload(Email.FromTrusted(root.GetProperty("email").GetString()!)),
                _ => throw new JsonException($"Неизвестный тип MFA payload: {type}"),
            };
        }

        public override void Write(Utf8JsonWriter writer, IMfaPayload value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("type", (int)value.Type);

            switch (value)
            {
                case TotpPayload totp:
                    writer.WriteString("secret", encryptor.Encrypt(totp.Secret));
                    break;
                case EmailPayload emailPayload:
                    writer.WriteString("email", emailPayload.Email.Value);
                    break;
                default:
                    throw new JsonException($"Unknown MFA payload type: {value.GetType()}");
            }

            writer.WriteEndObject();
        }
    }
}
