using Bookstore.Application.Common;
using Bookstore.Application.Settings;
using Bookstore.Infrastructure.Persistence;
using Bookstore.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddResponseCaching();

// Swagger Documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Bookstore API", 
        Version = "v1",
        Description = "A clean architecture Bookstore API built with .NET 10" 
    });

    // XML Documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // JWT Security Definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// Settings Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JWT"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<PaystackSettings>(builder.Configuration.GetSection("Paystack"));

// Register Infrastructure (this includes persistence, repos, and services)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var isTesting = builder.Environment.IsEnvironment("Testing");
builder.Services.AddInfrastructure(connectionString, isTesting);

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Key ?? ""))
    };
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://localhost:3000") // Common dev origins
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 20,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("orderPolicy", partitioner =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitioner.User.Identity?.Name ?? partitioner.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("authPolicy", partitioner =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitioner.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("emailPolicy", partitioner =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitioner.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(ApiResponse.ErrorResponse("Too many requests. Please try again later.", null, 429), token);
    };
});

var app = builder.Build();

app.UseCors("DefaultPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookstore API V1");
        c.RoutePrefix = "swagger";
    });
}
else 
{
    app.UseHttpsRedirection();
}

// Standard middleware
app.UseStaticFiles();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Database initialization - Use migrations for production (CRITICAL FIX)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            if (app.Environment.IsDevelopment())
            {
                // Development: Apply pending migrations (skip for InMemory)
                try
                {
                    if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
                    {
                        try
                        {
                            dbContext.Database.Migrate();
                            logger.LogInformation("Database migrations applied successfully");
                        }
                        catch (InvalidOperationException iex) when (iex.Message != null && iex.Message.Contains("PendingModelChangesWarning"))
                        {
                            logger.LogError(iex, "EF model has pending changes which require creating a new migration before applying database updates.");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Could not apply migrations automatically. Ensure database is contactable.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not determine database provider. Proceeding with application startup.");
                }
            }
            else
            {
                // Production: Apply migrations automatically (ensure database is ready)
                try
                {
                    dbContext.Database.Migrate();
                    logger.LogInformation("Production database migrations applied successfully");
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "CRITICAL: Could not apply production migrations");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database initialization scope");
        }
    }
}

app.Run();
