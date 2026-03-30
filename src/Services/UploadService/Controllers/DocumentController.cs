namespace UploadService.Controllers;

using Amazon.S3;
using Amazon.S3.Model;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Contracts.Events;

[ApiController]
[Route("api/upload")]
public class DocumentController : ControllerBase
{
    private readonly IAmazonS3 _s3Client;
    private readonly IPublishEndpoint _publishEndpoint;
    private const string BucketName = "documents";

    public DocumentController(IAmazonS3 s3Client, IPublishEndpoint publishEndpoint)
    {
        _s3Client = s3Client;
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No file uploaded");
        }

        foreach (var file in files)
        {
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
            await _s3Client.PutObjectAsync(putRequest);

            var blobUrl = $"http://localhost:9000/{BucketName}/{objectKey}";
            await _publishEndpoint.Publish(new DocumentUploaded(
                documentId,
                file.FileName,
                blobUrl,
                DateTime.UtcNow
            ));
        }

        return Ok(new { Message = "Files uploaded and event published" });
    }
}