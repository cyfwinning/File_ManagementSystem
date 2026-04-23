using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, ILogger logger)
    {
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Database ensured created.");

        // Apply incremental schema updates for existing databases
        await ApplySchemaUpdatesAsync(context, logger);

        if (!await context.SystemConfigs.AnyAsync())
        {
            context.SystemConfigs.AddRange(
                new SystemConfig { ConfigKey = "Watermark.Text", ConfigValue = "浙江南芯半导体，内部文件", Group = "Watermark" },
                new SystemConfig { ConfigKey = "Watermark.Enabled", ConfigValue = "true", Group = "Watermark" },
                new SystemConfig { ConfigKey = "Watermark.Opacity", ConfigValue = "0.15", Group = "Watermark" },
                new SystemConfig { ConfigKey = "Watermark.Angle", ConfigValue = "-25", Group = "Watermark" },
                new SystemConfig { ConfigKey = "Upload.MaxSizeMB", ConfigValue = "500", Group = "Upload" },
                new SystemConfig { ConfigKey = "Upload.AllowedExtensions", ConfigValue = ".doc,.docx,.xls,.xlsx,.ppt,.pptx,.pdf,.md,.png,.jpg,.jpeg,.gif,.bmp,.mp3,.m4a,.wav,.mp4,.mov,.avi,.flv", Group = "Upload" },
                new SystemConfig { ConfigKey = "Learning.ReportIntervalSeconds", ConfigValue = "10", Group = "Learning" },
                new SystemConfig { ConfigKey = "System.Version", ConfigValue = "1.0.0", Group = "System" }
            );
            await context.SaveChangesAsync();
            logger.LogInformation("System configs initialized.");
        }

        if (!await context.Users.AnyAsync())
        {
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                DisplayName = "超级管理员",
                Role = UserRole.SuperAdmin,
                IsActive = true
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("Default admin user created (admin/admin123).");
        }

        if (!await context.Departments.AnyAsync())
        {
            var rootDept = new Department { Name = "浙江南芯半导体", SortOrder = 0 };
            context.Departments.Add(rootDept);
            await context.SaveChangesAsync();

            context.Departments.AddRange(
                new Department { Name = "研发部", ParentId = rootDept.Id, SortOrder = 1 },
                new Department { Name = "产品部", ParentId = rootDept.Id, SortOrder = 2 },
                new Department { Name = "质量部", ParentId = rootDept.Id, SortOrder = 3 },
                new Department { Name = "制造部", ParentId = rootDept.Id, SortOrder = 4 },
                new Department { Name = "人事行政部", ParentId = rootDept.Id, SortOrder = 5 }
            );
            await context.SaveChangesAsync();
            logger.LogInformation("Default departments initialized.");
        }
    }

    /// <summary>
    /// Apply incremental schema updates for existing databases.
    /// EnsureCreated only creates the DB if it doesn't exist.
    /// This method adds missing columns and tables for schema changes made after initial creation.
    /// </summary>
    private static async Task ApplySchemaUpdatesAsync(AppDbContext context, ILogger logger)
    {
        var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        var isMySql = context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;

        if (!isSqlite && !isMySql)
            return;

        try
        {
            // Schema updates for Documents table
            await AddColumnIfNotExistsAsync(context, "Documents", "OriginalFileName", "TEXT", isSqlite, logger);
            await AddColumnIfNotExistsAsync(context, "Documents", "CoverUrl", "TEXT", isSqlite, logger);

            // Ensure AttachmentPermissions table exists
            await EnsureTableExistsAsync(context, "AttachmentPermissions", isSqlite, isMySql, logger);

            logger.LogInformation("Schema updates check completed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Some schema updates could not be applied. This is usually safe to ignore if the database was just created.");
        }
    }

    private static async Task<bool> HasColumnAsync(AppDbContext context, string tableName, string columnName, bool isSqlite)
    {
        if (isSqlite)
        {
            // Use raw ADO.NET for reliable SQLite PRAGMA queries
            var conn = context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName})";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        else
        {
            // MySQL
            var dbName = context.Database.GetDbConnection().Database;
            var conn = context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{dbName}' AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        }
    }

    private static async Task AddColumnIfNotExistsAsync(
        AppDbContext context, string tableName, string columnName, string columnType, bool isSqlite, ILogger logger)
    {
        if (await HasColumnAsync(context, tableName, columnName, isSqlite))
            return;

        var alterSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType} NULL";
        await context.Database.ExecuteSqlRawAsync(alterSql);
        logger.LogInformation("Added missing column {Column} to table {Table}.", columnName, tableName);
    }

    private static async Task<bool> HasTableAsync(AppDbContext context, string tableName, bool isSqlite)
    {
        var conn = context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        using var cmd = conn.CreateCommand();

        if (isSqlite)
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
        }
        else
        {
            var dbName = conn.Database;
            cmd.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{dbName}' AND TABLE_NAME = '{tableName}'";
        }

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result) > 0;
    }

    private static async Task EnsureTableExistsAsync(
        AppDbContext context, string tableName, bool isSqlite, bool isMySql, ILogger logger)
    {
        if (await HasTableAsync(context, tableName, isSqlite))
            return;

        if (isSqlite)
        {
            await context.Database.ExecuteSqlRawAsync($@"
                CREATE TABLE {tableName} (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AttachmentId INTEGER NOT NULL,
                    UserId INTEGER NULL,
                    DepartmentId INTEGER NULL,
                    CanPreview INTEGER NOT NULL DEFAULT 1,
                    CanDownload INTEGER NOT NULL DEFAULT 1,
                    GrantedById INTEGER NOT NULL,
                    IsDeleted INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    UpdatedAt TEXT NULL,
                    FOREIGN KEY (AttachmentId) REFERENCES Attachments(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GrantedById) REFERENCES Users(Id) ON DELETE RESTRICT
                )");
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync($@"
                CREATE TABLE {tableName} (
                    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    AttachmentId BIGINT NOT NULL,
                    UserId BIGINT NULL,
                    DepartmentId BIGINT NULL,
                    CanPreview TINYINT(1) NOT NULL DEFAULT 1,
                    CanDownload TINYINT(1) NOT NULL DEFAULT 1,
                    GrantedById BIGINT NOT NULL,
                    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
                    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                    UpdatedAt DATETIME(6) NULL,
                    FOREIGN KEY (AttachmentId) REFERENCES Attachments(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GrantedById) REFERENCES Users(Id) ON DELETE RESTRICT
                )");
        }
        logger.LogInformation("Created missing table {Table}.", tableName);
    }
}
