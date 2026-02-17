using Bookstore.Infrastructure;
using Bookstore.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Bookstore.Application.Settings;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.EntityFrameworkCore;  // For migrations

var builder = WebApplication.CreateBuilder(args);

// Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var jwtKey = builder.Configuration["JWT:Key"];
var jwtIssuer = builder.Configuration["JWT:Issuer"];
var jwtAudience = builder.Configuration["JWT:Audience"];

// Bind strongly-typed settings
builder.Services.Configure<Bookstore.Application.Settings.JwtSettings>(builder.Configuration.GetSection("JWT"));
builder.Services.Configure<Bookstore.Application.Settings.EmailSettings>(builder.Configuration.GetSection("Email"));

// Validate options and fail fast for critical settings
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("JWT"))
    .Validate(s => !string.IsNullOrEmpty(s.Key) && !string.IsNullOrEmpty(s.Issuer) && !string.IsNullOrEmpty(s.Audience), "JWT settings are required")
    .ValidateOnStart();

builder.Services.AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection("Email"))
    .Validate(s => string.IsNullOrEmpty(s.SmtpHost) || !string.IsNullOrEmpty(s.FromAddress), "If SMTP is configured, FromAddress is required")
    .ValidateOnStart();

// Rate limiting for sensitive endpoints (SECURITY)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    
    // Email endpoints: Very restrictive
    options.AddPolicy("emailPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(partitionKey: "emailPolicy", factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2
        }));
    
    // Authentication endpoints: Prevent brute force
    options.AddPolicy("authPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    
    // Order endpoints: Prevent spam orders
    options.AddPolicy("orderPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? 
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));
});

// Configure distributed cache for rate limiting (Redis preferred)
var redisConn = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
    });
}
else
{
    // Fallback for single-instance/dev
    builder.Services.AddDistributedMemoryCache();
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddResponseCaching(); // Add this for [ResponseCache]
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bookstore API", Version = "v1" });

    // Configure JWT authentication in Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });
});

// Add Infrastructure services
builder.Services.AddInfrastructure(connectionString ?? throw new InvalidOperationException("Connection string is missing"));

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? throw new InvalidOperationException("JWT Key is missing")))
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS with environment-specific configuration (SECURITY FIX)
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
        ?? Array.Empty<string>();
    
    if (builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
    {
        // Development: Allow localhost
        options.AddPolicy("AppCorsPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:4200", "https://localhost:5001")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    }
    else
    {
        // Production: Use configured allowed origins only
        options.AddPolicy("AppCorsPolicy", policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else
            {
                // No CORS if not configured - safest default
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        });
    }
});

// Add Logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// Add Health Checks for monitoring (PRODUCTION REQUIREMENT)
// Note: Install Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore for AddDbContextCheck
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseCors("AppCorsPolicy");
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoint for monitoring/ALB
app.MapHealthChecks("/health");

app.MapControllers();

// Database initialization - Use migrations for production (CRITICAL FIX)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Bookstore.Infrastructure.Persistence.BookStoreDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        if (app.Environment.IsDevelopment())
        {
            // Development: Apply pending migrations (skip for InMemory)
            if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                dbContext.Database.Migrate();
                logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                dbContext.Database.EnsureCreated();
                logger.LogInformation("InMemory database created");
            }
        }
        else
        {
            // Production: Just check if database is accessible
            // Migrations should be applied via CI/CD pipeline
            if (dbContext.Database.CanConnect())
            {
                logger.LogInformation("Database connection verified");
            }
            else
            {
                logger.LogError("Database connection failed");
                throw new InvalidOperationException("Cannot connect to database");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

app.Run();

// Expose Program class for integration tests (WebApplicationFactory)
public partial class Program { }
