using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpacesController : ControllerBase
{
    private readonly SpaceService _spaceService;

    public SpacesController(SpaceService spaceService) => _spaceService = spaceService;

    [HttpGet]
    public async Task<IActionResult> GetSpaces() => Ok(await _spaceService.GetSpacesAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSpaceRequest request)
    {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _spaceService.CreateSpaceAsync(request, userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSpaceRequest request)
    {
        var result = await _spaceService.UpdateSpaceAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _spaceService.DeleteSpaceAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
