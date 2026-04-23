using KnowledgeBase.Application;
using KnowledgeBase.Infrastructure;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Services;
using KnowledgeBase.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DDD Layer Services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "企业智能知识库 API", Version = "v1", Description = "浙江南芯半导体 - 企业智能知识库系统" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "KnowledgeBaseSecretKey2024!@#$%^&*()_+VeryLongKey";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "KnowledgeBase",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "KnowledgeBase",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // Support token from query string for file preview/download (iframe/a tags can't send headers)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Initialize all enabled databases
using (var scope = app.Services.CreateScope())
{
    var dbConfigManager = scope.ServiceProvider.GetRequiredService<DatabaseConfigManager>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var config = dbConfigManager.Load();

    // Initialize SQLite if enabled
    if (config.EnableSQLite)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite(config.SQLiteConnectionString);
            using var ctx = new AppDbContext(optionsBuilder.Options);
            await DbInitializer.InitializeAsync(ctx, logger);
            logger.LogInformation("SQLite database initialized successfully.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initialize SQLite database.");
        }
    }

    // Initialize MySQL if enabled
    if (config.EnableMySQL)
    {
        try
        {
            // Add short connection timeout for startup init to avoid blocking
            var mysqlConnStr = config.MySQLConnectionString;
            if (!mysqlConnStr.Contains("Connection Timeout", StringComparison.OrdinalIgnoreCase) &&
                !mysqlConnStr.Contains("ConnectionTimeout", StringComparison.OrdinalIgnoreCase))
                mysqlConnStr += ";Connection Timeout=5";
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySql(mysqlConnStr, new MySqlServerVersion(new Version(8, 0, 0)));
            using var ctx = new AppDbContext(optionsBuilder.Options);
            await DbInitializer.InitializeAsync(ctx, logger);
            logger.LogInformation("MySQL database initialized successfully.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initialize MySQL database. Check connection settings.");
        }
    }
}

// Middleware Pipeline
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "知识库 API v1"));
}

// Serve uploaded files
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Database session middleware - MUST be after auth so JWT claims are available
app.UseDatabaseMiddleware();

app.MapControllers();

app.Run();
