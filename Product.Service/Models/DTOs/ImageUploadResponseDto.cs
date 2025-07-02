namespace Product.Service.Models.DTOs;

public class ImageUploadResponseDto
{
    public string FileName { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
}