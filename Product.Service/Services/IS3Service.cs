using Product.Service.Models.DTOs;

namespace Product.Service.Services;

public interface IS3Service
{
    Task<ImageUploadResponseDto> UploadImageAsync(IFormFile file);
    Task<string> GetPreSignedDownloadUrlAsync(string key);
    Task DeleteImageAsync(string key);
}