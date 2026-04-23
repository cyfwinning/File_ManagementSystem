using KnowledgeBase.Infrastructure.Services;
using System.Text.Json;

namespace KnowledgeBase.WebApi.Middleware;

/// <summary>
/// Middleware that resolves the database type for the current request.
/// Priority: X-Database-Type header > JWT DatabaseType claim > default.
/// Also catches database connection errors and returns a friendly JSON response.
/// </summary>
public class DatabaseMiddleware
{
    private readonly RequestDelegate _next;

    public DatabaseMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DatabaseSession session, DatabaseConfigManager configManager)
    {
        var dbType = ResolveDbType(context, configManager);
        session.DatabaseType = dbType;

        try
        {
            await _next(context);
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            var errorResponse = new
            {
                success = false,
                message = $"数据库连接失败 ({dbType})，请检查数据库服务是否正常运行或联系管理员切换数据库类型。",
                databaseType = dbType
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }

    private static string ResolveDbType(HttpContext context, DatabaseConfigManager configManager)
    {
        var enabledTypes = configManager.GetEnabledTypes();
        var defaultType = configManager.GetDefaultType();

        // 1. Check X-Database-Type header
        if (context.Request.Headers.TryGetValue("X-Database-Type", out var headerVal))
        {
            var headerType = headerVal.ToString();
            if (enabledTypes.Contains(headerType, StringComparer.OrdinalIgnoreCase))
                return headerType;
        }

        // 2. Check JWT claim
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst("DatabaseType");
            if (claim != null && enabledTypes.Contains(claim.Value, StringComparer.OrdinalIgnoreCase))
                return claim.Value;
        }

        // 3. Fall back to default
        return defaultType;
    }

    private static bool IsDatabaseConnectionError(Exception ex)
    {
        // Check the full exception chain for MySQL/DB connection errors
        var current = ex;
        while (current != null)
        {
            var typeName = current.GetType().Name;
            if (typeName.Contains("MySqlException") ||
                typeName.Contains("SqliteException") ||
                current.Message.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase))
                return true;
            current = current.InnerException;
        }
        return false;
    }
}

public static class DatabaseMiddlewareExtensions
{
    public static IApplicationBuilder UseDatabaseMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DatabaseMiddleware>();
    }
}
