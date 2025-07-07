using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Microsoft.OpenApi.Models;
using Product.Service.Models.Configuration;
using Product.Service.Repositories;
using Product.Service.Services;

Console.WriteLine("=== STARTING PRODUCT SERVICE ===");
Console.WriteLine($"Timestamp: {DateTime.UtcNow}");

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== BUILDER CREATED ===");

// Only load environment variables - no appsettings needed for config
builder.Configuration.AddEnvironmentVariables();

// Configure AwsConfiguration from environment variables
builder.Services.Configure<AwsConfiguration>(options =>
{
    options.Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
    options.ProductsTableName = Environment.GetEnvironmentVariable("PRODUCTS_TABLE_NAME") ?? "Products";
    options.CategoriesTableName = Environment.GetEnvironmentVariable("CATEGORIES_TABLE_NAME") ?? "Categories";
    options.S3BucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") ?? throw new InvalidOperationException("S3_BUCKET_NAME is required");
    options.UserServiceBaseUrl = Environment.GetEnvironmentVariable("USER_SERVICE_BASE_URL") ?? throw new InvalidOperationException("USER_SERVICE_BASE_URL is required");
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
    builder.Services.AddAWSService<IAmazonS3>();
    builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();
    Console.WriteLine("=== AWS SERVICES REGISTERED ===");
}
catch (Exception ex)
{
    Console.WriteLine($"=== ERROR REGISTERING AWS SERVICES: {ex.Message} ===");
    throw;
}

// Add HTTP client for user service communication
builder.Services.AddHttpClient<IAuthService, AuthService>();

// Add custom services
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IS3Service, S3Service>();

Console.WriteLine("=== CUSTOM SERVICES REGISTERED ===");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Product Microservice API", Version = "v1" });
    
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Microservice API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseRouting();
app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapGet("/", () => "Product Microservice is running!");

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
