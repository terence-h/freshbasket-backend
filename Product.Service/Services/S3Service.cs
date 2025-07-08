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

    // public async Task<ImageUploadResponseDto> UploadImageAsync(IFormFile file)
    // {
    //     try
    //     {
    //         // Validate file
    //         ValidateFile(file);
    //
    //         // Generate unique key
    //         var fileExtension = Path.GetExtension(file.FileName).ToLower();
    //         var key = $"products/{Guid.NewGuid()}{fileExtension}";
    //
    //         logger.LogInformation($"Uploading {file.FileName} ({file.Length} bytes) to key: {key}");
    //
    //         // Read file into memory first
    //         await using var inputStream = file.OpenReadStream();
    //         using var memoryStream = new MemoryStream();
    //         await inputStream.CopyToAsync(memoryStream);
    //         memoryStream.Position = 0; // Reset position to beginning
    //
    //         logger.LogInformation($"Copied to memory stream. Size: {memoryStream.Length}");
    //
    //         // Upload to S3 using memory stream
    //         var request = new PutObjectRequest
    //         {
    //             BucketName = _config.S3BucketName,
    //             Key = key,
    //             InputStream = memoryStream,
    //             ContentType = file.ContentType,
    //             ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
    //             UseChunkEncoding = false,
    //             DisablePayloadSigning = false
    //         };
    //
    //         var response = await s3Client.PutObjectAsync(request);
    //
    //         logger.LogInformation($"S3 upload completed. Status: {response.HttpStatusCode}, ETag: {response.ETag}");
    //
    //         // Generate pre-signed URL for download
    //         var downloadUrl = await GetPreSignedDownloadUrlAsync(key);
    //
    //         return new ImageUploadResponseDto
    //         {
    //             FileName = file.FileName,
    //             Key = key,
    //             Url = downloadUrl,
    //             Size = file.Length,
    //             ContentType = file.ContentType
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogError(ex, "Error uploading image");
    //         throw;
    //     }
    // }

    public async Task<ImageUploadResponseDto> UploadImageAsync(IFormFile file)
    {
        try
        {
            logger.LogInformation("=== STARTING FILE UPLOAD DEBUG ===");

            // Validate file
            ValidateFile(file);

            // Log initial file state
            logger.LogInformation($"Original file details:");
            logger.LogInformation($"  - FileName: {file.FileName}");
            logger.LogInformation($"  - Length: {file.Length} bytes");
            logger.LogInformation($"  - ContentType: {file.ContentType}");
            logger.LogInformation($"  - Headers count: {file.Headers?.Count ?? 0}");

            // Generate unique key
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var key = $"products/{Guid.NewGuid()}{fileExtension}";
            logger.LogInformation($"Generated S3 key: {key}");

            // Test 1: Read file and verify integrity
            logger.LogInformation("=== TESTING FILE READ INTEGRITY ===");
            await using var testStream = file.OpenReadStream();
            var testBuffer = new byte[1024]; // Read first 1KB
            var testBytesRead = await testStream.ReadAsync(testBuffer, 0, testBuffer.Length);
            logger.LogInformation($"Test read - bytes read: {testBytesRead}");
            logger.LogInformation($"First 20 bytes: {Convert.ToHexString(testBuffer.Take(20).ToArray())}");

            // Read entire file to verify
            testStream.Position = 0;
            using var fullTestStream = new MemoryStream();
            await testStream.CopyToAsync(fullTestStream);
            logger.LogInformation($"Full file read size: {fullTestStream.Length} bytes");
            var fullFileHash =
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fullTestStream.ToArray()));
            logger.LogInformation($"Full file SHA256: {fullFileHash}");

            // Test 2: Fresh stream for actual upload
            logger.LogInformation("=== PREPARING UPLOAD STREAM ===");
            await using var inputStream = file.OpenReadStream();
            logger.LogInformation($"Fresh input stream length: {inputStream.Length}");
            logger.LogInformation($"Fresh input stream position: {inputStream.Position}");
            logger.LogInformation($"Fresh input stream can read: {inputStream.CanRead}");
            logger.LogInformation($"Fresh input stream can seek: {inputStream.CanSeek}");

            // Copy to memory stream with verification
            using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream);
            logger.LogInformation($"Memory stream length after copy: {memoryStream.Length}");
            logger.LogInformation($"Memory stream position after copy: {memoryStream.Position}");

            // Verify memory stream contents
            var memoryStreamHash =
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(memoryStream.ToArray()));
            logger.LogInformation($"Memory stream SHA256: {memoryStreamHash}");
            logger.LogInformation($"Hash match: {fullFileHash == memoryStreamHash}");

            if (fullFileHash != memoryStreamHash)
            {
                logger.LogError("FILE CORRUPTION DETECTED DURING STREAM COPY!");
                throw new InvalidOperationException("File corruption detected during stream copy");
            }

            // Reset position for upload
            memoryStream.Position = 0;
            logger.LogInformation($"Memory stream position reset to: {memoryStream.Position}");

            // Test 3: Alternative upload methods
            logger.LogInformation("=== TESTING MULTIPLE UPLOAD APPROACHES ===");

            // Approach 1: Basic upload
            var request1 = new PutObjectRequest
            {
                BucketName = _config.S3BucketName,
                Key = key + "_test1",
                InputStream = memoryStream,
                ContentType = file.ContentType
            };

            logger.LogInformation("Uploading with basic settings...");
            var response1 = await s3Client.PutObjectAsync(request1);
            logger.LogInformation($"Basic upload - Status: {response1.HttpStatusCode}, ETag: {response1.ETag}");

            // Verify upload 1
            var metadata1 = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _config.S3BucketName,
                Key = key + "_test1"
            });
            logger.LogInformation(
                $"Basic upload verification - S3 size: {metadata1.ContentLength}, Original: {file.Length}");

            // Reset stream for next test
            memoryStream.Position = 0;

            // Approach 2: With explicit headers
            var request2 = new PutObjectRequest
            {
                BucketName = _config.S3BucketName,
                Key = key + "_test2",
                InputStream = memoryStream,
                ContentType = file.ContentType,
                Headers =
                {
                    ContentLength = memoryStream.Length
                }
            };

            logger.LogInformation("Uploading with explicit content length...");
            var response2 = await s3Client.PutObjectAsync(request2);
            logger.LogInformation(
                $"Explicit headers upload - Status: {response2.HttpStatusCode}, ETag: {response2.ETag}");

            // Verify upload 2
            var metadata2 = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _config.S3BucketName,
                Key = key + "_test2"
            });
            logger.LogInformation(
                $"Explicit headers verification - S3 size: {metadata2.ContentLength}, Original: {file.Length}");

            // Reset stream for next test
            memoryStream.Position = 0;

            // Approach 3: Byte array upload
            var fileBytes = memoryStream.ToArray();
            logger.LogInformation($"File bytes array length: {fileBytes.Length}");

            var request3 = new PutObjectRequest
            {
                BucketName = _config.S3BucketName,
                Key = key + "_test3",
                InputStream = new MemoryStream(fileBytes),
                ContentType = file.ContentType,
            };

            logger.LogInformation("Uploading with fresh byte array stream...");
            var response3 = await s3Client.PutObjectAsync(request3);
            logger.LogInformation($"Byte array upload - Status: {response3.HttpStatusCode}, ETag: {response3.ETag}");

            // Verify upload 3
            var metadata3 = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _config.S3BucketName,
                Key = key + "_test3"
            });
            logger.LogInformation(
                $"Byte array verification - S3 size: {metadata3.ContentLength}, Original: {file.Length}");

            // Test 4: Download and verify one of the uploads
            logger.LogInformation("=== VERIFYING UPLOADED FILE INTEGRITY ===");
            var downloadRequest = new GetObjectRequest
            {
                BucketName = _config.S3BucketName,
                Key = key + "_test3" // Test the byte array version
            };

            using var downloadResponse = await s3Client.GetObjectAsync(downloadRequest);
            using var downloadStream = new MemoryStream();
            await downloadResponse.ResponseStream.CopyToAsync(downloadStream);

            var downloadedHash =
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(downloadStream.ToArray()));
            logger.LogInformation($"Downloaded file SHA256: {downloadedHash}");
            logger.LogInformation($"Downloaded vs Original hash match: {fullFileHash == downloadedHash}");
            logger.LogInformation($"Downloaded file size: {downloadStream.Length}");

            if (fullFileHash != downloadedHash)
            {
                logger.LogError("FILE CORRUPTION CONFIRMED - UPLOAD TO S3 CORRUPTED THE FILE!");

                // Log more details about the corruption
                var originalBytes = fullTestStream.ToArray();
                var downloadedBytes = downloadStream.ToArray();

                logger.LogError(
                    $"Original file first 100 bytes: {Convert.ToHexString(originalBytes.Take(100).ToArray())}");
                logger.LogError(
                    $"Downloaded file first 100 bytes: {Convert.ToHexString(downloadedBytes.Take(100).ToArray())}");

                // Find where corruption starts
                int corruptionIndex = -1;
                int maxCheck = Math.Min(originalBytes.Length, downloadedBytes.Length);
                for (int i = 0; i < maxCheck; i++)
                {
                    if (originalBytes[i] != downloadedBytes[i])
                    {
                        corruptionIndex = i;
                        break;
                    }
                }

                if (corruptionIndex >= 0)
                {
                    logger.LogError($"First corruption detected at byte index: {corruptionIndex}");
                }
                else if (originalBytes.Length != downloadedBytes.Length)
                {
                    logger.LogError(
                        $"File truncated - original: {originalBytes.Length}, downloaded: {downloadedBytes.Length}");
                }
            }
            else
            {
                logger.LogInformation("SUCCESS - File uploaded without corruption!");
            }

            // Use the successful upload (test3) as the final result
            var downloadUrl = await GetPreSignedDownloadUrlAsync(key + "_test3");

            logger.LogInformation("=== UPLOAD DEBUG COMPLETE ===");

            return new ImageUploadResponseDto
            {
                FileName = file.FileName,
                Key = key + "_test3",
                Url = downloadUrl,
                Size = file.Length,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in comprehensive upload debugging");
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