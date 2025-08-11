using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using API.Extensions;
using API.Middleware;
using Domain.Constants;
using Service.Implements;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize model validation error response
        options.SuppressMapClientErrors = true;
        options.SuppressModelStateInvalidFilter = false;
    });

// Add custom services
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddCustomAuthorization();
builder.Services.AddCustomSwagger();
builder.Services.AddDataAccessServices(builder.Configuration);
builder.Services.AddBusinessServices();
builder.Services.AddCustomCors(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);

// Add framework services
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// Add background services
builder.Services.AddHostedService<TokenCleanupBackgroundService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    builder.Logging.AddEventSourceLogger();
}

// Configure forwarded headers for reverse proxy scenarios
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "JWT API V1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    if (context.Request.IsHttps)
    {
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    
    await next();
});

// Exception handling (should be first)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Forwarded headers for reverse proxy
app.UseForwardedHeaders();

// HTTPS redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS
var corsPolicy = app.Environment.IsDevelopment() ? "AllowAll" : "AllowSpecificOrigins";
app.UseCors(corsPolicy);

// Response caching
app.UseResponseCaching();

// Authentication & Authorization
app.UseAuthentication();

// Custom JWT middleware
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseMiddleware<PermissionMiddleware>();

app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Controllers
app.MapControllers();

// Default redirect to Swagger in development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

// Database initialization and seeding
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Initialize database and seed data
        await InitializeDatabaseAsync(scope.ServiceProvider, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

app.Run();
static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, ILogger logger)
{
    logger.LogInformation("Initializing database...");
    
    var userRepository = serviceProvider.GetRequiredService<DataAccess.Interfaces.IUserRepository>();
    
    // Check if we need to seed data
    var userCount = await userRepository.CountAsync();
    if (userCount == 0)
    {
        logger.LogInformation("Seeding initial data...");
        await SeedInitialDataAsync(serviceProvider, logger);
    }
    
    logger.LogInformation("Database initialization completed");
}
static async Task SeedInitialDataAsync(IServiceProvider serviceProvider, ILogger logger)
{
    var userRepository = serviceProvider.GetRequiredService<DataAccess.Interfaces.IUserRepository>();
    var productRepository = serviceProvider.GetRequiredService<DataAccess.Interfaces.IProductRepository>();
    
    // Create default admin user
    var adminUser = new Domain.Entities.User
    {
        UserName = "admin",
        Email = "admin@example.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
        Permissions = PermissionConstants.SYSTEM_ADMIN_PERMISSIONS,
        AuthType = Domain.Enums.AuthenticationType.JWT,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    
    var adminId = await userRepository.InsertAsync(adminUser);
    logger.LogInformation("Created admin user with ID: {AdminId}", adminId);
    
    // Create default regular user
    var regularUser = new Domain.Entities.User
    {
        UserName = "user",
        Email = "user@example.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
        Permissions = PermissionConstants.BASIC_USER_PERMISSIONS,
        AuthType = Domain.Enums.AuthenticationType.JWT,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    
    var userId = await userRepository.InsertAsync(regularUser);
    logger.LogInformation("Created regular user with ID: {UserId}", userId);
    
    string ToJsonTags(params string[] tags) => JsonSerializer.Serialize(tags);

    var products = new[]
    {
        new Domain.Entities.Product
        {
            Name = "Laptop Pro Max",
            Description = "High-performance laptop for professional work and gaming",
            Price = 1299.99m,
            Stock = 25,
            CreatedBy = adminId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Tags = JsonSerializer.Serialize(new[] {"electronics", "computers", "gaming"})
        },
        new Domain.Entities.Product
        {
            Name = "Smartphone Ultra",
            Description = "Latest flagship smartphone with advanced camera system",
            Price = 899.99m,
            Stock = 50,
            CreatedBy = adminId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Tags = JsonSerializer.Serialize(new[] {"electronics", "mobile", "photography"})
        },
        new Domain.Entities.Product
        {
            Name = "Wireless Headphones",
            Description = "Premium noise-cancelling wireless headphones",
            Price = 249.99m,
            Stock = 75,
            CreatedBy = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Tags = JsonSerializer.Serialize(new[] {"electronics", "audio", "wireless"})
        },
        new Domain.Entities.Product
        {
            Name = "Smart Watch Series X",
            Description = "Advanced fitness tracking and smart features",
            Price = 399.99m,
            Stock = 30,
            CreatedBy = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Tags = JsonSerializer.Serialize(new[] {"electronics", "wearables", "fitness"})
        },
        new Domain.Entities.Product
        {
            Name = "4K Webcam",
            Description = "Ultra HD webcam for streaming and video calls",
            Price = 129.99m,
            Stock = 40,
            CreatedBy = adminId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Tags = JsonSerializer.Serialize(new[] {"electronics", "camera", "streaming"})
        }
    };
    
    foreach (var product in products)
    {
        try
        {
            var productId = await productRepository.InsertAsync(product);
            logger.LogInformation("Created product '{ProductName}' with ID: {ProductId}", product.Name, productId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to insert product '{ProductName}'", product.Name);
        }
    }
    
    logger.LogInformation("Initial data seeding completed");
}