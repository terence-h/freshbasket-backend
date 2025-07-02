namespace User.Service.Models.Configurations;

public class AwsConfiguration
{
    public string Region { get; set; } = string.Empty;
    public string DynamoDbTableName { get; set; } = string.Empty;
    public string JwtSecretKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public int JwtExpirationMinutes { get; set; } = 10800;
}