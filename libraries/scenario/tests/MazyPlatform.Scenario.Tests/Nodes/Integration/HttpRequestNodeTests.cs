namespace MazyPlatform.Scenario.Tests.Nodes.Integration;

using System.Net;
using System.Net.Http;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Integration;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

using ExecutionContext = MazyPlatform.Scenario.Abstractions.Execution.ExecutionContext;

public class HttpRequestNodeTests
{
    [Test]
    public async Task ExecuteAsync_SavesResponseBodyToVariable()
    {
        var factory = CreateFactoryReturning(HttpStatusCode.OK, "hello");
        var node = new HttpRequestNode(
            Guid.NewGuid(),
            url: "https://example.local",
            method: "GET",
            responseBodyVariable: "body",
            httpClientFactory: factory);
        var context = CreateContext();

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["body"]).IsEqualTo("hello");
    }

    [Test]
    public async Task ExecuteAsync_WithStatusVariable_SavesStatusCode()
    {
        var factory = CreateFactoryReturning(HttpStatusCode.Created, "{}");
        var node = new HttpRequestNode(
            Guid.NewGuid(),
            url: "https://example.local",
            method: "GET",
            responseBodyVariable: "body",
            httpClientFactory: factory,
            responseStatusVariable: "statusCode");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables["statusCode"]).IsEqualTo(201);
    }

    [Test]
    public async Task ExecuteAsync_WithoutStatusVariable_DoesNotCreateStatusVariable()
    {
        var factory = CreateFactoryReturning(HttpStatusCode.OK, "ok");
        var node = new HttpRequestNode(
            Guid.NewGuid(),
            url: "https://example.local",
            method: "GET",
            responseBodyVariable: "body",
            httpClientFactory: factory);
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables.Count).IsEqualTo(1);
        var hasStatusSuffix = context.Session.Variables.Keys.Any(
            k => k.EndsWith("_status", StringComparison.Ordinal));
        await Assert.That(hasStatusSuffix).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_DefaultBodyType_SendsApplicationJsonContentType()
    {
        var (factory, capture) = CreateCapturingFactory(HttpStatusCode.OK, "ok");
        var node = new HttpRequestNode(
            Guid.NewGuid(),
            url: "https://example.local",
            method: "POST",
            responseBodyVariable: "body",
            httpClientFactory: factory,
            body: "{\"x\":1}");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(capture.ContentType).IsEqualTo("application/json");
        await Assert.That(capture.Body).IsEqualTo("{\"x\":1}");
    }

    [Test]
    public async Task ExecuteAsync_FormBodyType_SendsFormUrlEncodedContentType()
    {
        var (factory, capture) = CreateCapturingFactory(HttpStatusCode.OK, "ok");
        var node = new HttpRequestNode(
            Guid.NewGuid(),
            url: "https://example.local",
            method: "POST",
            responseBodyVariable: "body",
            httpClientFactory: factory,
            body: "name=John&age=25",
            bodyType: "form");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(capture.ContentType).IsEqualTo("application/x-www-form-urlencoded");
        await Assert.That(capture.Body).IsEqualTo("name=John&age=25");
    }

    [Test]
    public async Task ExecuteAsync_RawBodyType_SendsTextPlainContentType()
    {
        var (factory, capture) = CreateCapturingFactory(HttpStatusCode.OK, "ok");
        var node = new HttpRequestNode(
            Guid.NewGuid(),
            url: "https://example.local",
            method: "POST",
            responseBodyVariable: "body",
            httpClientFactory: factory,
            body: "hello world",
            bodyType: "raw");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(capture.ContentType).IsEqualTo("text/plain");
        await Assert.That(capture.Body).IsEqualTo("hello world");
    }

    [Test]
    public async Task ExecuteAsync_JsonBodyType_WithoutBody_DoesNotSendContent()
    {
        var (factory, capture) = CreateCapturingFactory(HttpStatusCode.OK, "ok");
        var node = new HttpRequestNode(
            Guid.NewGuid(),
            url: "https://example.local",
            method: "POST",
            responseBodyVariable: "body",
            httpClientFactory: factory,
            bodyType: "json");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(capture.HasContent).IsFalse();
    }

    private static IHttpClientFactory CreateFactoryReturning(HttpStatusCode statusCode, string body)
    {
        var handler = new StubHandler(statusCode, body);
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);
        return factory;
    }

    private static (IHttpClientFactory Factory, RequestCapture Capture) CreateCapturingFactory(HttpStatusCode statusCode, string body)
    {
        var capture = new RequestCapture();
        var handler = new CapturingHandler(statusCode, body, capture);
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);
        return (factory, capture);
    }

    private static ExecutionContext CreateContext() =>
        new()
        {
            Session = new TestSession(),
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = string.Empty,
            ProjectId = Guid.NewGuid(),
        };

    private sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            });
    }

    private sealed class RequestCapture
    {
        public string? ContentType { get; set; }

        public string? Body { get; set; }

        public bool HasContent { get; set; }
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, string body, RequestCapture capture) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            capture.HasContent = request.Content is not null;
            capture.ContentType = request.Content?.Headers.ContentType?.MediaType;
            capture.Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            };
        }
    }
}
