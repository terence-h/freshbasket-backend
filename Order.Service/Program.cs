using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.OpenApi.Models;
using Order.Service.BackgroundServices;
using Order.Service.Models.Configurations;
using Order.Service.Repositories;
using Order.Service.Services;

Console.WriteLine("=== STARTING ORDER SERVICE ===");
Console.WriteLine($"Timestamp: {DateTime.UtcNow}");

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== BUILDER CREATED ===");

// Add configuration
builder.Services.Configure<AwsConfiguration>(options =>
{
    options.OrderProcessingQueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL") ?? throw new InvalidOperationException("SQS_QUEUE_URL is required");
    options.OrderNotificationQueueUrl = Environment.GetEnvironmentVariable("SNS_QUEUE_URL") ?? throw new InvalidOperationException("SNS_QUEUE_URL is required");
    options.OrderNotificationTopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN") ?? throw new InvalidOperationException("SNS_TOPIC_ARN is required");
    options.UserServiceBaseUrl = Environment.GetEnvironmentVariable("USER_SERVICE_BASE_URL") ?? throw new InvalidOperationException("USER_SERVICE_BASE_URL is required");
});

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine("=== CONFIGURATION LOADED ===");

// Configure AWS services - ECS provides credentials and region automatically
try 
{
    var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
    Console.WriteLine($"AWS Region: {awsRegion}");
    
    // Add AWS services
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddAWSService<IAmazonSQS>();
    builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
    builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>();

    // Add background services
    builder.Services.AddHostedService<OrderProcessingService>();
    builder.Services.AddHostedService<NotificationService>();
    Console.WriteLine("=== AWS SERVICES REGISTERED ===");
}
catch (Exception ex)
{
    Console.WriteLine($"=== ERROR REGISTERING AWS SERVICES: {ex.Message} ===");
    throw;
}

// Add HttpClient for User Service
builder.Services.AddHttpClient<IUserService, UserService>();

// Add custom services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISqsService, SqsService>();
builder.Services.AddScoped<ISnsService, SnsService>();
builder.Services.AddScoped<IUserService, UserService>();

Console.WriteLine("=== CUSTOM SERVICES REGISTERED ===");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order Microservice API", Version = "v1" });
    
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Microservice API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseRouting();
app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapGet("/", () => "Order Microservice is running!");

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
