using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using KnowledgeBase.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly DatabaseSession _databaseSession;
    private readonly DatabaseConfigManager _dbConfigManager;

    public AuthController(AuthService authService, DatabaseSession databaseSession, DatabaseConfigManager dbConfigManager)
    {
        _authService = authService;
        _databaseSession = databaseSession;
        _dbConfigManager = dbConfigManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Resolve the database type for this login request
        var enabledTypes = _dbConfigManager.GetEnabledTypes();
        var dbType = _dbConfigManager.GetDefaultType();

        if (!string.IsNullOrEmpty(request.DatabaseType) &&
            enabledTypes.Contains(request.DatabaseType, StringComparer.OrdinalIgnoreCase))
        {
            dbType = request.DatabaseType;
        }

        // Set session so DbContext resolves to the correct database
        _databaseSession.DatabaseType = dbType;

        var result = await _authService.LoginAsync(request, dbType);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Unauthenticated endpoint: returns available database types for the login page.
    /// </summary>
    [HttpGet("database-options")]
    public IActionResult GetDatabaseOptions()
    {
        var enabledTypes = _dbConfigManager.GetEnabledTypes();
        var defaultType = _dbConfigManager.GetDefaultType();
        return Ok(new DatabaseOptionsDto(enabledTypes, defaultType));
    }
}
