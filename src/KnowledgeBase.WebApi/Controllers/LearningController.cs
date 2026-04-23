using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LearningController : ControllerBase
{
    private readonly LearningService _learningService;

    public LearningController(LearningService learningService) => _learningService = learningService;

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats() => Ok(await _learningService.GetUserStatsAsync(GetUserId()));

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _learningService.GetUserRecordsAsync(GetUserId(), page, pageSize));

    [HttpPost("progress")]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest request)
    {
        var result = await _learningService.UpdateProgressAsync(request, GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
