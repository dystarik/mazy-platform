namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Database.Configurations;

using System.Text.Json;
using System.Text.Json.Serialization;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.Service.Bot.Manager.Domain.UserAccounts;
using MazyPlatform.Service.Bot.Manager.Infrastructure.Security;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BotInstanceConfiguration(IBotTokenEncryptor tokenEncryptor) : IEntityTypeConfiguration<BotInstance>
{
    private readonly JsonSerializerOptions _credentialsJsonOptions = new()
    {
        Converters = { new BotCredentialsJsonConverter(tokenEncryptor) },
    };

    public void Configure(EntityTypeBuilder<BotInstance> builder)
    {
        builder.ToTable("bot_instances");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        builder.Property(x => x.OwnerAccountId).HasColumnName("owner_account_id").IsRequired();
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.OwnerAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(x => x.ScenarioVersion).HasColumnName("scenario_version").IsRequired(false);
        builder.Property(x => x.ScenarioVersionUpdateMode)
            .HasColumnName("scenario_version_update_mode")
            .HasDefaultValue(ScenarioVersionUpdateMode.Auto)
            .IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();

        builder.Property(x => x.Credentials)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _credentialsJsonOptions),
                v => JsonSerializer.Deserialize<IBotCredentials>(v, _credentialsJsonOptions)!)
            .HasColumnName("credentials")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => x.OwnerAccountId);
        builder.HasIndex(x => x.ProjectId);

        builder.Ignore(x => x.PlatformType);
    }

    private sealed class BotCredentialsJsonConverter(IBotTokenEncryptor encryptor) : JsonConverter<IBotCredentials>
    {
        public override IBotCredentials Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var platformType = (PlatformType)root.GetProperty("platform_type").GetInt32();

            return platformType switch
            {
                PlatformType.Vk => new VkBotCredentials(
                    encryptor.Decrypt(root.GetProperty("access_token").GetString()!),
                    root.GetProperty("community_id").GetString()!),
                PlatformType.Telegram => new TelegramBotCredentials(
                    encryptor.Decrypt(root.GetProperty("access_token").GetString()!)),
                _ => throw new JsonException($"Неизвестный тип платформы: {platformType}"),
            };
        }

        public override void Write(Utf8JsonWriter writer, IBotCredentials value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("platform_type", (int)value.PlatformType);
            writer.WriteString("access_token", encryptor.Encrypt(value.AccessToken));

            switch (value)
            {
                case VkBotCredentials vk:
                    writer.WriteString("community_id", vk.CommunityId);
                    break;
                case TelegramBotCredentials:
                    break;
                default:
                    throw new JsonException($"Неизвестный тип credentials: {value.GetType()}");
            }

            writer.WriteEndObject();
        }
    }
}
