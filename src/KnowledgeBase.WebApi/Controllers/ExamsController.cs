using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly ExamService _examService;

    public ExamsController(ExamService examService) => _examService = examService;

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> GetExams([FromQuery] long? departmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _examService.GetExamsAsync(departmentId, page, pageSize));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetExam(long id) => Ok(await _examService.GetExamDetailAsync(id));

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,CompanyLeader,DepartmentLeader")]
    public async Task<IActionResult> Create([FromBody] CreateExamRequest request)
    {
        var result = await _examService.CreateExamAsync(request, GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitExamRequest request, [FromQuery] int timeSpentSeconds = 0)
    {
        var result = await _examService.SubmitExamAsync(request, GetUserId(), timeSpentSeconds);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
