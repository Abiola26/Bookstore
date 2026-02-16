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

// Rate limiting for sensitive endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("emailPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(partitionKey: "emailPolicy", factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
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

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

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
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Database migration
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Bookstore.Infrastructure.Persistence.BookStoreDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

// Expose Program class for integration tests (WebApplicationFactory)
public partial class Program { }
