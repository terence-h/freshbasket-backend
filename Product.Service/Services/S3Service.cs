using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Product.Service.Models.Configuration;
using Product.Service.Models.DTOs;

namespace Product.Service.Services;

public class S3Service(IAmazonS3 s3Client, IOptions<AwsConfiguration> config, ILogger<S3Service> logger) : IS3Service
{
    private readonly AwsConfiguration _config = config.Value;
    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private readonly string[] _allowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];

    public async Task<ImageUploadResponseDto> UploadImageAsync(IFormFile file)
    {
        try
        {
            // Validate file
            ValidateFile(file);

            // Generate unique key
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var key = $"products/{Guid.NewGuid()}{fileExtension}";

            // Upload to S3
            await using var stream = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = _config.S3BucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            await s3Client.PutObjectAsync(request);

            // Generate pre-signed URL for download
            var downloadUrl = await GetPreSignedDownloadUrlAsync(key);

            return new ImageUploadResponseDto
            {
                FileName = file.FileName,
                Key = key,
                Url = downloadUrl,
                Size = file.Length,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image");
            throw;
        }
    }

    public async Task<string> GetPreSignedDownloadUrlAsync(string key)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _config.S3BucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(1)
            };

            return await s3Client.GetPreSignedURLAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating pre-signed URL for key: {Key}", key);
            throw;
        }
    }

    public async Task DeleteImageAsync(string key)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _config.S3BucketName,
                Key = key
            };

            await s3Client.DeleteObjectAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting image with key: {Key}", key);
            throw;
        }
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required");

        if (file.Length > 2 * 1024 * 1024) // 2MB limit
            throw new ArgumentException("File size must be less than 2MB");

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
            throw new ArgumentException($"File extension {extension} is not allowed");

        if (!_allowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException($"Content type {file.ContentType} is not allowed");
    }
}
