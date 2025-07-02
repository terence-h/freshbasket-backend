namespace Product.Service.Models.Configuration;

public class AwsConfiguration
{
    public string Region { get; set; } = string.Empty;
    public string ProductsTableName { get; set; } = string.Empty;
    public string CategoriesTableName { get; set; } = string.Empty;
    public string S3BucketName { get; set; } = string.Empty;
    public string UserServiceBaseUrl { get; set; } = string.Empty;
}