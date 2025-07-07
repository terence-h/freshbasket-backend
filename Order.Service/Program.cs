using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Order.Service.BackgroundServices;
using Order.Service.Models.Configurations;
using Order.Service.Repositories;
using Order.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add configuration
builder.Services.Configure<AwsConfiguration>(options =>
{
    options.OrderProcessingQueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL") ?? throw new InvalidOperationException("SQS_QUEUE_URL is required");
    options.OrderNotificationQueueUrl = Environment.GetEnvironmentVariable("SNS_QUEUE_URL") ?? throw new InvalidOperationException("SNS_QUEUE_URL is required");
    options.OrderNotificationTopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN") ?? throw new InvalidOperationException("SNS_TOPIC_ARN is required");
    options.UserServiceBaseUrl = Environment.GetEnvironmentVariable("USER_SERVICE_BASE_URL") ?? throw new InvalidOperationException("USER_SERVICE_BASE_URL is required");
});

// Add AWS services
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>();

// Add HttpClient for User Service
builder.Services.AddHttpClient<IUserService, UserService>();

// Add custom services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISqsService, SqsService>();
builder.Services.AddScoped<ISnsService, SnsService>();
builder.Services.AddScoped<IUserService, UserService>();

// Add background services
builder.Services.AddHostedService<OrderProcessingService>();
builder.Services.AddHostedService<NotificationService>();

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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();