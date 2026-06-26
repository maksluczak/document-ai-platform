using Moq;
using Amazon.S3;
using MassTransit;
using UploadService.Controllers;
using Microsoft.AspNetCore.Http;
using Contracts.Events;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;

public class DocumentControllerTests
{
    [Fact]
    public async Task Upload_WhenFileIsUploaded_ShouldPutFileInBucketAndPublish()
    {
        var mockS3Client = new Mock<IAmazonS3>();
        var mockPublishEndpoint = new Mock<IPublishEndpoint>();
        var mockFileValidator = new Mock<IFileValidator>();

        mockFileValidator
            .Setup(v => v.IsValidExtension(It.IsAny<string>()))
            .Returns(true);

        var controller = new DocumentController(
            mockS3Client.Object,
            mockPublishEndpoint.Object,
            mockFileValidator.Object
        );

        var mockFile = new Mock<IFormFile>();

        var stream = new MemoryStream(new byte[] { });

        mockFile.Setup(f => f.FileName).Returns("test.pdf");
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        var files = new FormFileCollection { mockFile.Object };

        mockS3Client.Setup(x => x.PutObjectAsync(
                It.IsAny<PutObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        mockPublishEndpoint.Setup(x => x.Publish(
                It.IsAny<DocumentUploaded>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await controller.Upload(files);

        result.Should().BeOfType<OkObjectResult>();

        mockS3Client.Verify(x => x.PutObjectAsync(
                It.Is<PutObjectRequest>(r =>
                    r.BucketName == "documents" &&
                    r.Key.Contains("test.pdf")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockPublishEndpoint.Verify(x => x.Publish(
                It.IsAny<DocumentUploaded>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}