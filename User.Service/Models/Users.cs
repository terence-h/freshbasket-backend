namespace User.Service.Models;

using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [DynamoDBProperty]
    public string Email { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string PasswordHash { get; set; } = string.Empty;

    [DynamoDBProperty]
    public List<string> Roles { get; set; } = [];

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DynamoDBProperty]
    public DateTime? LastLoginAt { get; set; }

    [DynamoDBProperty]
    public bool IsActive { get; set; } = true;
}