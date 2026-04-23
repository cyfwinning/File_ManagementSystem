using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly RecommendationService _recService;

    public RecommendationsController(RecommendationService recService) => _recService = recService;

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRecommendations() => Ok(await _recService.GetRecommendationsForUserAsync(GetUserId()));

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,CompanyLeader,DepartmentLeader")]
    public async Task<IActionResult> Create([FromBody] CreateRecommendationRequest request)
    {
        var result = await _recService.CreateRecommendationAsync(request, GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
