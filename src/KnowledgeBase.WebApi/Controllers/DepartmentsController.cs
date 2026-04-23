using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _departmentService;

    public DepartmentsController(DepartmentService departmentService) => _departmentService = departmentService;

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree() => Ok(await _departmentService.GetDepartmentTreeAsync());

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        var result = await _departmentService.CreateDepartmentAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDepartmentRequest request)
    {
        var result = await _departmentService.UpdateDepartmentAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _departmentService.DeleteDepartmentAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
