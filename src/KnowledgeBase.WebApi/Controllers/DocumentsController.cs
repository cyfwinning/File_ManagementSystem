using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeBase.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;

    public DocumentsController(DocumentService documentService) => _documentService = documentService;

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private string GetRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";
    private long? GetDepartmentId()
    {
        var v = User.FindFirst("DepartmentId")?.Value;
        return v != null ? long.Parse(v) : null;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] long? spaceId, [FromQuery] long? categoryId, [FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _documentService.GetDocumentsAsync(spaceId, categoryId, keyword, page, pageSize));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(long id) => Ok(await _documentService.GetDocumentByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequest request)
    {
        var result = await _documentService.CreateDocumentAsync(request, GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDocumentRequest request)
    {
        var result = await _documentService.UpdateDocumentAsync(id, request, GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _documentService.DeleteDocumentAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/pin")]
    public async Task<IActionResult> TogglePin(long id) => Ok(await _documentService.TogglePinAsync(id));

    [HttpPost("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(long id) => Ok(await _documentService.ToggleFavoriteAsync(id, GetUserId()));

    // Versions
    [HttpGet("{id}/versions")]
    public async Task<IActionResult> GetVersions(long id) => Ok(await _documentService.GetVersionsAsync(id));

    // Comments
    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(long id) => Ok(await _documentService.GetCommentsAsync(id));

    [HttpPost("comments")]
    public async Task<IActionResult> AddComment([FromBody] CreateCommentRequest request)
    {
        var result = await _documentService.AddCommentAsync(request, GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Attachments
    [HttpGet("{id}/attachments")]
    public async Task<IActionResult> GetAttachments(long id) => Ok(await _documentService.GetAttachmentsAsync(id));

    [HttpPost("{id}/attachments")]
    public async Task<IActionResult> UploadAttachment(long id, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("请选择文件");
        using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAttachmentAsync(id, stream, file.FileName, file.ContentType, file.Length, GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Attachment download
    [HttpGet("attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(long attachmentId)
    {
        var perm = await _documentService.CheckAttachmentPermissionAsync(attachmentId, GetUserId(), GetDepartmentId(), GetRole());
        if (!perm.canDownload) return Forbid();
        var file = await _documentService.GetAttachmentFileAsync(attachmentId);
        if (file == null) return NotFound();
        return File(file.Value.stream!, file.Value.contentType ?? "application/octet-stream", file.Value.fileName);
    }

    // Attachment preview (inline)
    [HttpGet("attachments/{attachmentId}/preview")]
    public async Task<IActionResult> PreviewAttachment(long attachmentId)
    {
        var perm = await _documentService.CheckAttachmentPermissionAsync(attachmentId, GetUserId(), GetDepartmentId(), GetRole());
        if (!perm.canPreview) return Forbid();
        var file = await _documentService.GetAttachmentFileAsync(attachmentId);
        if (file == null) return NotFound();
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{file.Value.fileName}\"";
        return File(file.Value.stream!, file.Value.contentType ?? "application/octet-stream");
    }

    // Delete attachment
    [HttpDelete("attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(long attachmentId)
    {
        var result = await _documentService.DeleteAttachmentAsync(attachmentId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Attachment permissions
    [HttpGet("attachments/{attachmentId}/permissions")]
    public async Task<IActionResult> GetAttachmentPermissions(long attachmentId)
        => Ok(await _documentService.GetAttachmentPermissionsAsync(attachmentId));

    [HttpPost("attachments/permissions")]
    public async Task<IActionResult> SetAttachmentPermission([FromBody] SetAttachmentPermissionRequest request)
    {
        var result = await _documentService.SetAttachmentPermissionAsync(request, GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("attachments/permissions/{permissionId}")]
    public async Task<IActionResult> DeleteAttachmentPermission(long permissionId)
    {
        var result = await _documentService.DeleteAttachmentPermissionAsync(permissionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
