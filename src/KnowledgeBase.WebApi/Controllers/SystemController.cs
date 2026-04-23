using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Interfaces;
using KnowledgeBase.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SystemController : ControllerBase
{
    private readonly IRepository<SystemConfig> _configRepo;
    private readonly IRepository<OperationLog> _logRepo;
    private readonly DatabaseConfigManager _dbConfigManager;

    public SystemController(IRepository<SystemConfig> configRepo, IRepository<OperationLog> logRepo, DatabaseConfigManager dbConfigManager)
    {
        _configRepo = configRepo;
        _logRepo = logRepo;
        _dbConfigManager = dbConfigManager;
    }

    [HttpGet("configs")]
    public async Task<IActionResult> GetConfigs()
    {
        var configs = await _configRepo.GetAllAsync();
        return Ok(configs.Select(c => new SystemConfigDto(c.Id, c.ConfigKey, c.ConfigValue, c.Description, c.Group)));
    }

    [HttpPut("configs/{id}")]
    public async Task<IActionResult> UpdateConfig(long id, [FromBody] string value)
    {
        var config = await _configRepo.GetByIdAsync(id);
        if (config == null) return NotFound();
        config.ConfigValue = value;
        await _configRepo.UpdateAsync(config);
        return Ok(new { success = true });
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _logRepo.Query().OrderByDescending(l => l.CreatedAt);
        var total = await query.CountAsync();
        var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { items = logs, totalCount = total, page, pageSize });
    }

    // ========== Database Configuration Management ==========

    /// <summary>
    /// Get the current database availability configuration (from dbconfig.json).
    /// </summary>
    [HttpGet("database/config")]
    public IActionResult GetDatabaseConfig()
    {
        var config = _dbConfigManager.Load();
        return Ok(new
        {
            enableSQLite = config.EnableSQLite,
            enableMySQL = config.EnableMySQL,
            defaultType = config.DefaultType,
            sqliteConnectionString = config.SQLiteConnectionString,
            mysqlConnectionString = MaskConnectionString(config.MySQLConnectionString)
        });
    }

    /// <summary>
    /// Update database availability settings.
    /// </summary>
    [HttpPost("database/config")]
    public IActionResult ConfigureDatabase([FromBody] UpdateDatabaseConfigRequest request)
    {
        var config = _dbConfigManager.Load();

        if (request.EnableSQLite.HasValue) config.EnableSQLite = request.EnableSQLite.Value;
        if (request.EnableMySQL.HasValue) config.EnableMySQL = request.EnableMySQL.Value;
        if (!string.IsNullOrEmpty(request.DefaultType)) config.DefaultType = request.DefaultType;
        if (!string.IsNullOrEmpty(request.SQLiteConnectionString)) config.SQLiteConnectionString = request.SQLiteConnectionString;
        if (!string.IsNullOrEmpty(request.MySQLConnectionString)) config.MySQLConnectionString = request.MySQLConnectionString;

        // Ensure at least one database is enabled
        if (!config.EnableSQLite && !config.EnableMySQL)
        {
            config.EnableSQLite = true;
        }

        // Ensure default type is one of the enabled types
        if (config.DefaultType == "MySQL" && !config.EnableMySQL)
            config.DefaultType = "SQLite";
        if (config.DefaultType == "SQLite" && !config.EnableSQLite)
            config.DefaultType = "MySQL";

        _dbConfigManager.Save(config);

        return Ok(new { success = true, message = "数据库配置已保存，立即生效" });
    }

    /// <summary>
    /// Test MySQL connection.
    /// </summary>
    [HttpPost("database/test-mysql")]
    public IActionResult TestMySqlConnection([FromBody] TestMySqlRequest request)
    {
        try
        {
            var connStr = request.ConnectionString;
            if (string.IsNullOrEmpty(connStr))
            {
                var config = _dbConfigManager.Load();
                connStr = config.MySQLConnectionString;
            }

            var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Infrastructure.Persistence.AppDbContext>();
            optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 0)));
            using var ctx = new Infrastructure.Persistence.AppDbContext(optionsBuilder.Options);
            ctx.Database.CanConnect();

            return Ok(new { success = true, message = "MySQL 连接成功" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = $"MySQL 连接失败: {ex.Message}" });
        }
    }

    private static string MaskConnectionString(string connStr)
    {
        // Mask password in connection string for display
        if (string.IsNullOrEmpty(connStr)) return connStr;
        try
        {
            var parts = connStr.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase) ||
                    parts[i].TrimStart().StartsWith("Pwd", StringComparison.OrdinalIgnoreCase))
                {
                    var eqIdx = parts[i].IndexOf('=');
                    if (eqIdx >= 0)
                        parts[i] = parts[i][..(eqIdx + 1)] + "******";
                }
            }
            return string.Join(";", parts);
        }
        catch
        {
            return "******";
        }
    }
}

public record UpdateDatabaseConfigRequest(
    bool? EnableSQLite,
    bool? EnableMySQL,
    string? DefaultType,
    string? SQLiteConnectionString,
    string? MySQLConnectionString
);

public record TestMySqlRequest(string? ConnectionString);
