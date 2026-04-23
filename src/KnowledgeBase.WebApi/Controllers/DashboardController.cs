using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService) => _dashboardService = dashboardService;

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private string GetRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";
    private long? GetDepartmentId()
    {
        var deptClaim = User.FindFirst("DepartmentId")?.Value;
        return deptClaim != null ? long.Parse(deptClaim) : null;
    }

    /// <summary>Personal dashboard stats (year/week/7days)</summary>
    [HttpGet("personal")]
    public async Task<IActionResult> GetPersonalStats()
        => Ok(await _dashboardService.GetPersonalStatsAsync(GetUserId()));

    /// <summary>Trend chart data</summary>
    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend([FromQuery] string period = "7day")
        => Ok(await _dashboardService.GetTrendAsync(GetUserId(), period, GetRole(), GetDepartmentId()));

    /// <summary>Department dashboard (for leaders)</summary>
    [HttpGet("department")]
    public async Task<IActionResult> GetDepartmentDashboard()
    {
        var deptId = GetDepartmentId();
        if (deptId == null) return BadRequest(new { success = false, message = "用户未分配部门" });
        return Ok(await _dashboardService.GetDepartmentDashboardAsync(deptId.Value));
    }

    /// <summary>Specific user stats (for leaders)</summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserStats(long userId)
    {
        var role = GetRole();
        if (role is not ("SuperAdmin" or "CompanyLeader" or "DepartmentLeader"))
            return Forbid();
        return Ok(await _dashboardService.GetUserStatsDetailAsync(userId));
    }
}
