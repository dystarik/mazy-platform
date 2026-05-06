namespace MazyPlatform.Scenario.Tests.Nodes.Integration;

using System.Net.Http;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Integration;

using NSubstitute;

public class HttpRequestNodeDescriptorTests
{
    [Test]
    public async Task Schema_MethodParameter_IsEnumWithFiveValues()
    {
        var descriptor = new HttpRequestNodeDescriptor();

        var method = descriptor.Schema.Single(
            p => string.Equals(p.Key, "method", StringComparison.Ordinal));

        await Assert.That(method.Type).IsEqualTo(NodeParamType.Enum);
        await Assert.That(method.EnumValues).IsNotNull();
        await Assert.That(method.EnumValues!.Count).IsEqualTo(5);
        await Assert.That(method.EnumValues).Contains("GET");
        await Assert.That(method.EnumValues).Contains("POST");
        await Assert.That(method.EnumValues).Contains("PUT");
        await Assert.That(method.EnumValues).Contains("PATCH");
        await Assert.That(method.EnumValues).Contains("DELETE");
    }

    [Test]
    public async Task Validate_WithKnownMethod_ReturnsNoErrors()
    {
        var descriptor = new HttpRequestNodeDescriptor();
        var parameters = ParseJson("""
            {
              "url": "https://example.local",
              "method": "POST",
              "responseBodyVariable": "body"
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithoutMethod_ReturnsNoErrors()
    {
        var descriptor = new HttpRequestNodeDescriptor();
        var parameters = ParseJson("""
            {
              "url": "https://example.local",
              "responseBodyVariable": "body"
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithUnknownMethod_ReturnsEnumError()
    {
        var descriptor = new HttpRequestNodeDescriptor();
        var parameters = ParseJson("""
            {
              "url": "https://example.local",
              "method": "BANANA",
              "responseBodyVariable": "body"
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"method\"");
        await Assert.That(errors[0]).Contains("GET");
        await Assert.That(errors[0]).Contains("DELETE");
    }

    [Test]
    public async Task Create_WithoutMethod_DefaultsToGet()
    {
        var descriptor = new HttpRequestNodeDescriptor();
        var parameters = ParseJson("""
            {
              "url": "https://example.local",
              "responseBodyVariable": "body"
            }
            """);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IHttpClientFactory)).Returns(Substitute.For<IHttpClientFactory>());

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<HttpRequestNode>();
        await Assert.That(node.NodeType).IsEqualTo("http_request");
    }

    [Test]
    public async Task Create_WithPatchMethod_CreatesNode()
    {
        var descriptor = new HttpRequestNodeDescriptor();
        var parameters = ParseJson("""
            {
              "url": "https://example.local",
              "method": "PATCH",
              "responseBodyVariable": "body"
            }
            """);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IHttpClientFactory)).Returns(Substitute.For<IHttpClientFactory>());

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<HttpRequestNode>();
    }

    [Test]
    public async Task Schema_BodyTypeParameter_IsEnumWithThreeValues()
    {
        var descriptor = new HttpRequestNodeDescriptor();

        var bodyType = descriptor.Schema.Single(
            p => string.Equals(p.Key, "bodyType", StringComparison.Ordinal));

        await Assert.That(bodyType.Type).IsEqualTo(NodeParamType.Enum);
        await Assert.That(bodyType.IsRequired).IsFalse();
        await Assert.That(bodyType.EnumValues).IsNotNull();
        await Assert.That(bodyType.EnumValues!.Count).IsEqualTo(3);
        await Assert.That(bodyType.EnumValues).Contains("json");
        await Assert.That(bodyType.EnumValues).Contains("form");
        await Assert.That(bodyType.EnumValues).Contains("raw");
    }

    [Test]
    public async Task Validate_WithKnownBodyType_ReturnsNoErrors()
    {
        var descriptor = new HttpRequestNodeDescriptor();
        var parameters = ParseJson("""
            {
              "url": "https://example.local",
              "method": "POST",
              "responseBodyVariable": "body",
              "body": "name=John",
              "bodyType": "form"
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithUnknownBodyType_ReturnsEnumError()
    {
        var descriptor = new HttpRequestNodeDescriptor();
        var parameters = ParseJson("""
            {
              "url": "https://example.local",
              "responseBodyVariable": "body",
              "bodyType": "banana"
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("bodyType");
        await Assert.That(errors[0]).Contains("json");
        await Assert.That(errors[0]).Contains("form");
        await Assert.That(errors[0]).Contains("raw");
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;
}
