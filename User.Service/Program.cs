using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using User.Service.Models.Configurations;
using User.Service.Repositories;
using User.Service.Services;

Console.WriteLine("=== STARTING USER SERVICE ===");
Console.WriteLine($"Timestamp: {DateTime.UtcNow}");

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== BUILDER CREATED ===");

// Only load environment variables - no appsettings needed for config
builder.Configuration.AddEnvironmentVariables();

// Configure AwsConfiguration from environment variables
builder.Services.Configure<AwsConfiguration>(options =>
{
    options.Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
    options.DynamoDbTableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME") ?? "Users";
    options.JwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? throw new InvalidOperationException("JWT_SECRET_KEY is required");
    options.JwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new InvalidOperationException("JWT_ISSUER is required");
    options.JwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new InvalidOperationException("JWT_AUDIENCE is required");
    options.JwtExpirationMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINS") ?? "10800");
});

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine("=== CONFIGURATION LOADED ===");
    
// Configure AWS services - ECS provides credentials and region automatically
try 
{
    var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
    Console.WriteLine($"AWS Region: {awsRegion}");
    
    // ECS Fargate handles AWS credentials automatically
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();
    Console.WriteLine("=== AWS SERVICES REGISTERED ===");
}
catch (Exception ex)
{
    Console.WriteLine($"=== ERROR REGISTERING AWS SERVICES: {ex.Message} ===");
    throw;
}

// Add custom services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
Console.WriteLine("=== CUSTOM SERVICES REGISTERED ===");

// Configure JWT authentication from environment variables
Console.WriteLine("=== CONFIGURING JWT ===");
try 
{
    var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
    var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
    
    Console.WriteLine($"JWT Key: {(string.IsNullOrEmpty(jwtKey) ? "NULL/EMPTY" : $"Found ({jwtKey.Length} chars)")}");
    Console.WriteLine($"JWT Issuer: {jwtIssuer ?? "NULL"}");
    Console.WriteLine($"JWT Audience: {jwtAudience ?? "NULL"}");
    
    if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
    {
        throw new InvalidOperationException("JWT configuration incomplete - check environment variables");
    }
    
    var key = Encoding.ASCII.GetBytes(jwtKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
    Console.WriteLine("=== JWT AUTHENTICATION CONFIGURED ===");
}
catch (Exception ex)
{
    Console.WriteLine($"=== ERROR CONFIGURING JWT: {ex.Message} ===");
    throw;
}

builder.Services.AddAuthorization();
Console.WriteLine("=== AUTHORIZATION CONFIGURED ===");

builder.Services.AddControllers();
Console.WriteLine("=== CONTROLLERS ADDED ===");

builder.Services.AddEndpointsApiExplorer();
Console.WriteLine("=== ENDPOINTS API EXPLORER ADDED ===");

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Microservice API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});
Console.WriteLine("=== SWAGGER CONFIGURED ===");

builder.Services.AddHealthChecks();
Console.WriteLine("=== HEALTH CHECKS ADDED ===");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
Console.WriteLine("=== CORS CONFIGURED ===");

Console.WriteLine("=== BUILDING APPLICATION ===");
var app = builder.Build();
Console.WriteLine("=== APPLICATION BUILT SUCCESSFULLY ===");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Microservice API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => "User Microservice is running!");

Console.WriteLine("=== STARTING APPLICATION ===");
Console.WriteLine($"Listening on: http://+:8080");

try 
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"=== APPLICATION FAILED: {ex.Message} ===");
    throw;
}