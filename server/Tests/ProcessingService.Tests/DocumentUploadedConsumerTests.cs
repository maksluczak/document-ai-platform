using Amazon.S3;
using Amazon.S3.Model;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessingService.Analysis;
using ProcessingService.Classifier;
using ProcessingService.Consumers;
using Xunit;

namespace ProcessingService.Tests.Consumers;

public class DocumentUploadedConsumerTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock = new();
    private readonly Mock<IDocumentAnalysisService> _analysisServiceMock = new();
    private readonly Mock<IDocumentClassifier> _classifierMock = new();
    private readonly Mock<ConsumeContext<DocumentUploaded>> _contextMock = new();

    private readonly DocumentUploaded _message = new(
        Guid.NewGuid(),
        "invoice.pdf",
        "https://blob.local/invoice.pdf",
        new DateTime());

    private DocumentUploadedConsumer CreateSut() => new(
        Mock.Of<ILogger<DocumentUploadedConsumer>>(),
        _s3ClientMock.Object,
        _analysisServiceMock.Object,
        _classifierMock.Object);

    private void SetupS3Object()
    {
        var responseStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var response = new GetObjectResponse { ResponseStream = responseStream };

        _s3ClientMock
            .Setup(s => s.GetObjectAsync("documents", $"{_message.DocumentId}-{_message.FileName}", default))
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task Consume_PublishesDocumentProcessed_WithStructuredFields_WhenDocumentIsRecognized()
    {
        SetupS3Object();

        var readResult = FakeAnalyzeResult.WithLines("Faktura VAT 123/2026");
        _analysisServiceMock
            .Setup(a => a.AnalyzeAsync("prebuilt-read", It.IsAny<Stream>()))
            .ReturnsAsync(readResult);

        _classifierMock
            .Setup(c => c.ClassifyAsync(It.IsAny<string>()))
            .ReturnsAsync(new ClassificationResult("invoice", 0.95f, "prebuilt-invoice"));

        var invoiceResult = FakeAnalyzeResult.WithDocumentFields(
            "invoice",
            new Dictionary<string, string> { ["Total"] = "1234.56 PLN" });

        _analysisServiceMock
            .Setup(a => a.AnalyzeAsync("prebuilt-invoice", It.IsAny<Stream>()))
            .ReturnsAsync(invoiceResult);

        _contextMock.Setup(c => c.Message).Returns(_message);

        DocumentProcessed? published = null;
        _contextMock
            .Setup(c => c.Publish(It.IsAny<DocumentProcessed>(), default))
            .Callback<DocumentProcessed, CancellationToken>((evt, _) => published = evt)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.Consume(_contextMock.Object);

        Assert.NotNull(published);
        Assert.Equal(_message.DocumentId, published!.DocumentId);
        Assert.Equal("invoice", published.DocumentType);
        Assert.Equal("1234.56 PLN", published.ExtractedFields["Total"]);

        _analysisServiceMock.Verify(a => a.AnalyzeAsync("prebuilt-invoice", It.IsAny<Stream>()), Times.Once);
        _analysisServiceMock.Verify(a => a.AnalyzeAsync("prebuilt-read", It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public async Task Consume_SkipsSecondAnalyzeCall_WhenClassifierReturnsPrebuiltRead()
    {
        SetupS3Object();

        var readResult = FakeAnalyzeResult.WithLines("Nieustrukturyzowany tekst bez pol.");
        _analysisServiceMock
            .Setup(a => a.AnalyzeAsync("prebuilt-read", It.IsAny<Stream>()))
            .ReturnsAsync(readResult);

        _classifierMock
            .Setup(c => c.ClassifyAsync(It.IsAny<string>()))
            .ReturnsAsync(new ClassificationResult("other", 0.3f, "prebuilt-read"));

        _contextMock.Setup(c => c.Message).Returns(_message);
        _contextMock
            .Setup(c => c.Publish(It.IsAny<DocumentProcessed>(), default))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.Consume(_contextMock.Object);

        // tylko jedno wywołanie Azure — wynik z odczytu jest reużyty
        _analysisServiceMock.Verify(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public async Task Consume_RethrowsAndDoesNotPublish_WhenS3Fails()
    {
        _s3ClientMock
            .Setup(s => s.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("bucket not found"));

        _contextMock.Setup(c => c.Message).Returns(_message);

        var sut = CreateSut();

        await Assert.ThrowsAsync<AmazonS3Exception>(() => sut.Consume(_contextMock.Object));

        _contextMock.Verify(c => c.Publish(It.IsAny<DocumentProcessed>(), default), Times.Never);
    }
}

internal static class FakeAnalyzeResult
{
    public static AnalyzeResult WithLines(string text) =>
        DocumentAnalysisModelFactory.AnalyzeResult(
            pages: new List<DocumentPage>
            {
                DocumentAnalysisModelFactory.DocumentPage(
                    lines: new List<DocumentLine>
                    {
                        DocumentAnalysisModelFactory.DocumentLine(content: text)
                    })
            },
            documents: new List<AnalyzedDocument>());

    public static AnalyzeResult WithDocumentFields(string documentType, Dictionary<string, string> fields)
    {
        var documentFields = fields.ToDictionary(
            f => f.Key,
            f =>
            {
                DocumentFieldValue fieldValue = DocumentAnalysisModelFactory.DocumentFieldValueWithStringFieldType(f.Value);

                return DocumentAnalysisModelFactory.DocumentField(
                    fieldType: DocumentFieldType.String,
                    value: fieldValue,
                    content: f.Value,
                    boundingRegions: Array.Empty<BoundingRegion>(),
                    spans: Array.Empty<DocumentSpan>(),
                    confidence: 0.99f);
            });

        var document = DocumentAnalysisModelFactory.AnalyzedDocument(
            documentType: documentType,
            fields: documentFields);

        return DocumentAnalysisModelFactory.AnalyzeResult(
            pages: new List<DocumentPage>(),
            documents: new List<AnalyzedDocument> { document });
    }
}