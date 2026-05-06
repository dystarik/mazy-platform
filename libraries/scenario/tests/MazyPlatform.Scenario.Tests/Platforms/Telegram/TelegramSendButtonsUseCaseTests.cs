namespace MazyPlatform.Scenario.Tests.Platforms.Telegram;

using System.Net;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Telegram.Api;
using MazyPlatform.Scenario.Telegram.Configuration;
using MazyPlatform.Scenario.Telegram.UseCases;
using MazyPlatform.Scenario.Tests.Helpers;

using Microsoft.Extensions.Options;

using NSubstitute;

using ExecutionContext = MazyPlatform.Scenario.Abstractions.Execution.ExecutionContext;

public class TelegramSendButtonsUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_BuildsInlineKeyboardRowsAndStyles()
    {
        var (useCase, capture) = CreateUseCase("""{"ok":true,"result":{"message_id":123}}""");
        var layout = new ButtonLayout(
        [
            [
                new ButtonDefinition("Да", "yes", ButtonStyle.Success),
                new ButtonDefinition("Нет", "no", ButtonStyle.Danger),
            ],
            [
                new ButtonDefinition("Назад", "back", ButtonStyle.Secondary),
                new ButtonDefinition("Далее", "next", ButtonStyle.Primary),
            ],
        ]);

        await useCase.ExecuteAsync(CreateContext(), "Выберите действие", layout);

        using var body = JsonDocument.Parse(capture.Body!);
        var rows = body.RootElement
            .GetProperty("reply_markup")
            .GetProperty("inline_keyboard");

        await Assert.That(rows.GetArrayLength()).IsEqualTo(2);
        await Assert.That(rows[0].GetArrayLength()).IsEqualTo(2);
        await Assert.That(rows[0][0].GetProperty("style").GetString()).IsEqualTo("success");
        await Assert.That(rows[0][1].GetProperty("style").GetString()).IsEqualTo("danger");
        await Assert.That(rows[1][0].TryGetProperty("style", out _)).IsFalse();
        await Assert.That(rows[1][1].GetProperty("style").GetString()).IsEqualTo("primary");
    }

    [Test]
    public async Task ExecuteAsync_ValidatesPayloadLengthAcrossAllRows()
    {
        var (useCase, _) = CreateUseCase("""{"ok":true,"result":{"message_id":123}}""");
        var layout = new ButtonLayout(
        [
            [new ButtonDefinition("Ок", "ok")],
            [new ButtonDefinition("Слишком длинно", new string('x', 65))],
        ]);

        await Assert
            .That(async () => await useCase.ExecuteAsync(CreateContext(), "Выберите действие", layout))
            .ThrowsExactly<InvalidOperationException>();
    }

    private static (TelegramSendButtonsUseCase UseCase, RequestCapture Capture) CreateUseCase(string responseJson)
    {
        var capture = new RequestCapture();
        var handler = new CapturingHandler(HttpStatusCode.OK, responseJson, capture);
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);

        var apiClient = new TelegramApiClient(
            factory,
            Options.Create(new TelegramOptions { BaseUrl = "https://telegram.test", FileBaseUrl = "https://files.test" }));

        return (new TelegramSendButtonsUseCase(apiClient), capture);
    }

    private static ExecutionContext CreateContext() =>
        new()
        {
            Session = new TestSession(),
            IncomingEvent = new TestIncomingEvent { ChatId = "test_chat" },
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = "test_token",
            ProjectId = Guid.NewGuid(),
        };

    private sealed class RequestCapture
    {
        public string? Body { get; set; }
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, string responseBody, RequestCapture capture) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            capture.Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody),
            };
        }
    }
}
