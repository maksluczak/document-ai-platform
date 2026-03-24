using Amazon.S3;
using Amazon.S3.Model;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Contracts.Events;

namespace UploadService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IAmazonS3 s3Client;
    private readonly IPublishEndpoint publishEndpoint;
    private const string BucketName = "documents";

    public DocumentController(IAmazonS3 s3Client, IPublishEndpoint publishEndpoint)
    {
        this.s3Client = s3Client;
        this.publishEndpoint = publishEndpoint;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        var documentId = Guid.NewGuid();
        var objectKey = $"{documentId}-{file.FileName}";

        using var stream = file.OpenReadStream();
        var putRequest = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = file.ContentType
        };
        await s3Client.PutObjectAsync(putRequest);

        var blobUrl = $"http://localhost:9000/{BucketName}/{objectKey}";
        await publishEndpoint.Publish(new DocumentUploaded(
            documentId,
            file.FileName,
            blobUrl,
            DateTime.UtcNow
        ));

        return Ok(new { DocumentId = documentId, Message = "File uploaded and event published" });
    }
}