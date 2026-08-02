using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ProcessingService.Classifier;
using Xunit;

namespace ProcessingService.Tests.Classifier;

public class DocumentClassifierTests
{
    private static DocumentClassifier CreateSut(
        HttpStatusCode statusCode,
        object? responseBody,
        out Mock<HttpMessageHandler> handlerMock,
        out List<string> capturedRequestBodies)
    {
        var capturedBodies = new List<string>();
        capturedRequestBodies = capturedBodies;

        handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                if (req.Content is not null)
                {
                    capturedBodies.Add(await req.Content.ReadAsStringAsync());
                }

                var response = new HttpResponseMessage(statusCode);
                if (responseBody is not null)
                {
                    response.Content = JsonContent(responseBody);
                }

                return response;
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://classifier.local")
        };

        return new DocumentClassifier(httpClient, Mock.Of<ILogger<DocumentClassifier>>());
    }

    private static HttpContent JsonContent(object body) =>
        new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task ClassifyAsync_ReturnsClassifiedType_WhenConfidenceIsHigh()
    {
        var sut = CreateSut(
            HttpStatusCode.OK,
            new { documentType = "invoice", confidence = 0.92f, azureModel = "prebuilt-invoice" },
            out _,
            out _);

        var result = await sut.ClassifyAsync("Faktura VAT nr 123/2026 ...");

        Assert.Equal("invoice", result.DocumentType);
        Assert.Equal("prebuilt-invoice", result.AzureModel);
        Assert.Equal(0.92f, result.Confidence);
    }

    [Fact]
    public async Task ClassifyAsync_FallsBackToOther_WhenConfidenceBelowThreshold()
    {
        var sut = CreateSut(
            HttpStatusCode.OK,
            new { documentType = "hr", confidence = 0.4f, azureModel = "prebuilt-document" },
            out _,
            out _);

        var result = await sut.ClassifyAsync("Jakiś niejednoznaczny tekst dokumentu.");

        Assert.Equal("other", result.DocumentType);
        Assert.Equal("prebuilt-document", result.AzureModel);
        Assert.Equal(0.4f, result.Confidence);
    }

    [Fact]
    public async Task ClassifyAsync_Throws_WhenServiceReturnsError()
    {
        var sut = CreateSut(HttpStatusCode.InternalServerError, null, out _, out _);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.ClassifyAsync("dowolny tekst wejsciowy dluzszy niz dziesiec znakow"));
    }

    [Fact]
    public async Task ClassifyAsync_TruncatesTextTo2000Characters_BeforeSendingRequest()
    {
        var longText = new string('a', 5000);
        var sut = CreateSut(
            HttpStatusCode.OK,
            new { documentType = "other", confidence = 0.9f, azureModel = "prebuilt-read" },
            out _,
            out var capturedBodies);

        await sut.ClassifyAsync(longText);

        Assert.Single(capturedBodies);
        using var doc = JsonDocument.Parse(capturedBodies[0]);
        var sentText = doc.RootElement.GetProperty("text").GetString();
        Assert.NotNull(sentText);
        Assert.Equal(2000, sentText!.Length);
    }
}