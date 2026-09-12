using System.Net;
using System.Text;
using System.Text.Json;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Exceptions;
using KiwiCart.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace KiwiCart.Tests.Infrastructure;

public class GeminiClientTests
{
    private static readonly GeminiOptions ValidOptions = new()
    {
        ApiKey = "test-key",
        Model = "gemini-1.5-flash",
        MaxOutputTokens = 256,
        Temperature = 0.2
    };

    [Fact]
    public async Task GenerateContentAsync_ReturnsText_OnSuccess()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[] { new { text = "Milk, eggs, flour" } }
                    }
                }
            }
        });

        var client = CreateClient(HttpStatusCode.OK, responseJson, ValidOptions);

        var result = await client.GenerateContentAsync("What do I need for pancakes?");

        Assert.Equal("Milk, eggs, flour", result);
    }

    [Fact]
    public async Task GenerateContentAsync_ConcatenatesMultipleParts()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[] { new { text = "Hello " }, new { text = "world" } }
                    }
                }
            }
        });

        var client = CreateClient(HttpStatusCode.OK, responseJson, ValidOptions);

        var result = await client.GenerateContentAsync("greet");

        Assert.Equal("Hello world", result);
    }

    [Fact]
    public async Task GenerateContentAsync_LogsTokenUsage_WhenUsageMetadataPresent()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = "ok" } } } }
            },
            usageMetadata = new
            {
                promptTokenCount = 12,
                candidatesTokenCount = 8,
                totalTokenCount = 20
            }
        });

        var logger = new Mock<ILogger<GeminiClient>>();
        var client = CreateClient(HttpStatusCode.OK, responseJson, ValidOptions, logger.Object);

        await client.GenerateContentAsync("hi");

        // Verify an Information-level log carrying the token counts was emitted.
        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("token usage")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateContentAsync_Succeeds_WhenUsageMetadataMissing()
    {
        // No usageMetadata block: must not throw, still returns the text.
        var responseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = "no usage" } } } }
            }
        });

        var client = CreateClient(HttpStatusCode.OK, responseJson, ValidOptions);

        var result = await client.GenerateContentAsync("hi");

        Assert.Equal("no usage", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateContentAsync_Throws_OnEmptyPrompt(string prompt)
    {
        var client = CreateClient(HttpStatusCode.OK, "{}", ValidOptions);

        await Assert.ThrowsAsync<GeminiApiException>(
            () => client.GenerateContentAsync(prompt));
    }

    [Fact]
    public async Task GenerateContentAsync_Throws_WhenApiKeyMissing()
    {
        var options = new GeminiOptions { ApiKey = "", Model = "gemini-1.5-flash" };
        var client = CreateClient(HttpStatusCode.OK, "{}", options);

        await Assert.ThrowsAsync<GeminiApiException>(
            () => client.GenerateContentAsync("hi"));
    }

    [Fact]
    public async Task GenerateContentAsync_Throws_OnNonSuccessStatus()
    {
        var client = CreateClient(HttpStatusCode.TooManyRequests, "{\"error\":\"quota\"}", ValidOptions);

        await Assert.ThrowsAsync<GeminiApiException>(
            () => client.GenerateContentAsync("hi"));
    }

    [Fact]
    public async Task GenerateContentAsync_Throws_WhenNoCandidates()
    {
        var responseJson = JsonSerializer.Serialize(new { candidates = Array.Empty<object>() });
        var client = CreateClient(HttpStatusCode.OK, responseJson, ValidOptions);

        await Assert.ThrowsAsync<GeminiApiException>(
            () => client.GenerateContentAsync("hi"));
    }

    private static GeminiClient CreateClient(
        HttpStatusCode status, string content, GeminiOptions options,
        ILogger<GeminiClient>? logger = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(GeminiClient.HttpClientName)).Returns(httpClient);

        return new GeminiClient(
            factory.Object,
            Options.Create(options),
            logger ?? NullLogger<GeminiClient>.Instance);
    }
}
