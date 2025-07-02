namespace User.Service.Repositories;

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Models;

public class UserRepository(IDynamoDBContext dynamoDbContext, ILogger<UserRepository> logger) : IUserRepository
{
    public async Task<User?> GetByIdAsync(string id)
    {
        try
        {
            return await dynamoDbContext.LoadAsync<User>(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user by ID {UserId}", id);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            var scanConditions = new List<ScanCondition>
            {
                new("Email", ScanOperator.Equal, email)
            };

            var search = dynamoDbContext.ScanAsync<User>(scanConditions);
            var users = await search.GetNextSetAsync();
            return users.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user by email {Email}", email);
            throw;
        }
    }

    public async Task<User> CreateAsync(User user)
    {
        try
        {
            await dynamoDbContext.SaveAsync(user);
            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<User> UpdateAsync(User user)
    {
        try
        {
            await dynamoDbContext.SaveAsync(user);
            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            await dynamoDbContext.DeleteAsync<User>(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user {UserId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            var search = dynamoDbContext.ScanAsync<User>(new List<ScanCondition>());
            return await search.GetRemainingAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all users");
            throw;
        }
    }
}