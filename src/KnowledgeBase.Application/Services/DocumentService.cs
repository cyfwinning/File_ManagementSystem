using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Services;

public class DocumentService
{
    private readonly IRepository<Document> _docRepo;
    private readonly IRepository<DocumentVersion> _versionRepo;
    private readonly IRepository<Attachment> _attachRepo;
    private readonly IRepository<DocumentComment> _commentRepo;
    private readonly IRepository<Favorite> _favoriteRepo;
    private readonly IRepository<AttachmentPermission> _permRepo;
    private readonly IFileStorageService _fileStorage;

    public DocumentService(
        IRepository<Document> docRepo,
        IRepository<DocumentVersion> versionRepo,
        IRepository<Attachment> attachRepo,
        IRepository<DocumentComment> commentRepo,
        IRepository<Favorite> favoriteRepo,
        IRepository<AttachmentPermission> permRepo,
        IFileStorageService fileStorage)
    {
        _docRepo = docRepo;
        _versionRepo = versionRepo;
        _attachRepo = attachRepo;
        _commentRepo = commentRepo;
        _favoriteRepo = favoriteRepo;
        _permRepo = permRepo;
        _fileStorage = fileStorage;
    }

    public async Task<ApiResponse<PagedResult<DocumentListDto>>> GetDocumentsAsync(long? spaceId = null, long? categoryId = null, string? keyword = null, int page = 1, int pageSize = 20)
    {
        var query = _docRepo.Query().Include(d => d.Author).AsQueryable();
        if (spaceId.HasValue) query = query.Where(d => d.SpaceId == spaceId);
        if (categoryId.HasValue) query = query.Where(d => d.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(d => d.Title.Contains(keyword) || (d.Content != null && d.Content.Contains(keyword)) || (d.Tags != null && d.Tags.Contains(keyword)));

        var total = await query.CountAsync();
        var docs = await query
            .OrderByDescending(d => d.IsPinned).ThenByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DocumentListDto(d.Id, d.Title, d.OriginalFileName, d.CoverUrl, d.Summary, d.Status.ToString(), d.Author != null ? d.Author.DisplayName : null, d.Tags, d.IsPinned, d.ViewCount, d.FavoriteCount, d.CreatedAt, d.UpdatedAt))
            .ToListAsync();

        return ApiResponse<PagedResult<DocumentListDto>>.Ok(new PagedResult<DocumentListDto> { Items = docs, TotalCount = total, Page = page, PageSize = pageSize });
    }

    public async Task<ApiResponse<DocumentDto>> GetDocumentByIdAsync(long id)
    {
        var doc = await _docRepo.Query()
            .Include(d => d.Author).Include(d => d.Category).Include(d => d.Space)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return ApiResponse<DocumentDto>.Fail("文档不存在");

        doc.ViewCount++;
        await _docRepo.UpdateAsync(doc);

        return ApiResponse<DocumentDto>.Ok(new DocumentDto(doc.Id, doc.Title, doc.OriginalFileName, doc.CoverUrl, doc.Content, doc.Summary, doc.Status.ToString(), doc.EditMode.ToString(), doc.CategoryId, doc.Category?.Name, doc.SpaceId, doc.Space?.Name, doc.AuthorId, doc.Author?.DisplayName, doc.Tags, doc.IsPinned, doc.ViewCount, doc.FavoriteCount, doc.EditCount, doc.CurrentVersion, doc.CreatedAt, doc.UpdatedAt));
    }

    public async Task<ApiResponse<DocumentDto>> CreateDocumentAsync(CreateDocumentRequest request, long userId)
    {
        var doc = new Document
        {
            Title = request.Title,
            OriginalFileName = request.OriginalFileName,
            Content = request.Content,
            Summary = request.Summary,
            EditMode = request.EditMode,
            CategoryId = request.CategoryId,
            SpaceId = request.SpaceId,
            AuthorId = userId,
            Tags = request.Tags,
            Status = DocumentStatus.Published,
            CurrentVersion = 1
        };
        await _docRepo.AddAsync(doc);

        await _versionRepo.AddAsync(new DocumentVersion
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            Content = request.Content,
            EditorId = userId,
            ChangeNote = "初始版本"
        });

        return ApiResponse<DocumentDto>.Ok(new DocumentDto(doc.Id, doc.Title, doc.OriginalFileName, doc.CoverUrl, doc.Content, doc.Summary, doc.Status.ToString(), doc.EditMode.ToString(), doc.CategoryId, null, doc.SpaceId, null, doc.AuthorId, null, doc.Tags, doc.IsPinned, doc.ViewCount, doc.FavoriteCount, doc.EditCount, doc.CurrentVersion, doc.CreatedAt, doc.UpdatedAt));
    }

    public async Task<ApiResponse> UpdateDocumentAsync(long id, UpdateDocumentRequest request, long userId)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc == null) return ApiResponse.Fail("文档不存在");

        doc.Title = request.Title;
        doc.Content = request.Content;
        doc.Summary = request.Summary;
        doc.Tags = request.Tags;
        doc.EditCount++;
        doc.CurrentVersion++;
        await _docRepo.UpdateAsync(doc);

        await _versionRepo.AddAsync(new DocumentVersion
        {
            DocumentId = doc.Id,
            VersionNumber = doc.CurrentVersion,
            Content = request.Content,
            EditorId = userId,
            ChangeNote = request.ChangeNote ?? $"第{doc.CurrentVersion}次编辑"
        });

        return ApiResponse.Ok("更新成功");
    }

    public async Task<ApiResponse> DeleteDocumentAsync(long id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc == null) return ApiResponse.Fail("文档不存在");
        await _docRepo.DeleteAsync(doc);
        return ApiResponse.Ok("删除成功");
    }

    public async Task<ApiResponse> TogglePinAsync(long id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc == null) return ApiResponse.Fail("文档不存在");
        doc.IsPinned = !doc.IsPinned;
        await _docRepo.UpdateAsync(doc);
        return ApiResponse.Ok(doc.IsPinned ? "已置顶" : "已取消置顶");
    }

    public async Task<ApiResponse> ToggleFavoriteAsync(long documentId, long userId)
    {
        var existing = (await _favoriteRepo.FindAsync(f => f.UserId == userId && f.DocumentId == documentId)).FirstOrDefault();
        if (existing != null)
        {
            await _favoriteRepo.DeleteAsync(existing);
            var doc = await _docRepo.GetByIdAsync(documentId);
            if (doc != null) { doc.FavoriteCount = Math.Max(0, doc.FavoriteCount - 1); await _docRepo.UpdateAsync(doc); }
            return ApiResponse.Ok("已取消收藏");
        }

        await _favoriteRepo.AddAsync(new Favorite { UserId = userId, DocumentId = documentId });
        var document = await _docRepo.GetByIdAsync(documentId);
        if (document != null) { document.FavoriteCount++; await _docRepo.UpdateAsync(document); }
        return ApiResponse.Ok("已收藏");
    }

    // === Versions ===
    public async Task<ApiResponse<List<DocumentVersion>>> GetVersionsAsync(long documentId)
    {
        var versions = await _versionRepo.Query()
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
        return ApiResponse<List<DocumentVersion>>.Ok(versions);
    }

    // === Comments ===
    public async Task<ApiResponse<List<CommentDto>>> GetCommentsAsync(long documentId)
    {
        var comments = await _commentRepo.Query()
            .Where(c => c.DocumentId == documentId && c.ParentId == null)
            .Include(c => c.User).Include(c => c.Replies).ThenInclude(r => r.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = comments.Select(c => MapComment(c)).ToList();
        return ApiResponse<List<CommentDto>>.Ok(result);
    }

    public async Task<ApiResponse<CommentDto>> AddCommentAsync(CreateCommentRequest request, long userId)
    {
        var comment = new DocumentComment
        {
            DocumentId = request.DocumentId,
            UserId = userId,
            Content = request.Content,
            ParentId = request.ParentId,
            MentionedUserIds = request.MentionedUserIds
        };
        await _commentRepo.AddAsync(comment);
        return ApiResponse<CommentDto>.Ok(new CommentDto(comment.Id, comment.DocumentId, comment.UserId, null, comment.Content, comment.ParentId, comment.MentionedUserIds, comment.CreatedAt, null));
    }

    // === Attachments ===
    public async Task<ApiResponse<List<AttachmentDto>>> GetAttachmentsAsync(long documentId)
    {
        var attachments = await _attachRepo.Query()
            .Where(a => a.DocumentId == documentId)
            .Include(a => a.Uploader)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<AttachmentDto>>.Ok(attachments.Select(a =>
            new AttachmentDto(a.Id, a.OriginalFileName, a.ContentType, a.FileExtension, a.FileSize, a.Type.ToString(), a.DocumentId, a.Uploader?.DisplayName, _fileStorage.GetFileUrl(a.StoragePath), a.CreatedAt)).ToList());
    }

    public async Task<ApiResponse<AttachmentDto>> UploadAttachmentAsync(long documentId, Stream fileStream, string fileName, string contentType, long fileSize, long userId)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var attachType = ext switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" => AttachmentType.Image,
            ".mp3" or ".m4a" or ".wav" or ".ogg" => AttachmentType.Audio,
            ".mp4" or ".mov" or ".avi" or ".flv" or ".webm" => AttachmentType.Video,
            ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".pdf" or ".md" => AttachmentType.Document,
            _ => AttachmentType.Other
        };

        var storagePath = await _fileStorage.SaveFileAsync(fileStream, fileName, $"attachments/{documentId}");

        var attachment = new Attachment
        {
            FileName = Path.GetFileName(storagePath),
            OriginalFileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileExtension = ext,
            FileSize = fileSize,
            Type = attachType,
            DocumentId = documentId,
            UploaderId = userId
        };
        await _attachRepo.AddAsync(attachment);

        // Auto-set document cover from first attachment if not set yet
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc != null && string.IsNullOrEmpty(doc.CoverUrl))
        {
            doc.CoverUrl = _fileStorage.GetFileUrl(storagePath);
            await _docRepo.UpdateAsync(doc);
        }

        return ApiResponse<AttachmentDto>.Ok(new AttachmentDto(attachment.Id, attachment.OriginalFileName, attachment.ContentType, attachment.FileExtension, attachment.FileSize, attachment.Type.ToString(), attachment.DocumentId, null, _fileStorage.GetFileUrl(attachment.StoragePath), attachment.CreatedAt));
    }

    // === Attachment Download ===
    public async Task<(Stream? stream, string? fileName, string? contentType)?> GetAttachmentFileAsync(long attachmentId)
    {
        var attachment = await _attachRepo.GetByIdAsync(attachmentId);
        if (attachment == null) return null;
        var stream = await _fileStorage.GetFileAsync(attachment.StoragePath);
        if (stream == null) return null;
        return (stream, attachment.OriginalFileName, attachment.ContentType);
    }

    // === Attachment Permissions ===
    public async Task<ApiResponse<List<AttachmentPermissionDto>>> GetAttachmentPermissionsAsync(long attachmentId)
    {
        var perms = await _permRepo.Query()
            .Where(p => p.AttachmentId == attachmentId)
            .Include(p => p.User).Include(p => p.Department)
            .ToListAsync();

        return ApiResponse<List<AttachmentPermissionDto>>.Ok(perms.Select(p =>
            new AttachmentPermissionDto(p.Id, p.AttachmentId, null, p.UserId, p.User?.DisplayName, p.DepartmentId, p.Department?.Name, p.CanPreview, p.CanDownload)).ToList());
    }

    public async Task<ApiResponse> SetAttachmentPermissionAsync(SetAttachmentPermissionRequest request, long grantedById)
    {
        var existing = await _permRepo.Query()
            .Where(p => p.AttachmentId == request.AttachmentId && p.UserId == request.UserId && p.DepartmentId == request.DepartmentId)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.CanPreview = request.CanPreview;
            existing.CanDownload = request.CanDownload;
            await _permRepo.UpdateAsync(existing);
        }
        else
        {
            await _permRepo.AddAsync(new AttachmentPermission
            {
                AttachmentId = request.AttachmentId,
                UserId = request.UserId,
                DepartmentId = request.DepartmentId,
                CanPreview = request.CanPreview,
                CanDownload = request.CanDownload,
                GrantedById = grantedById
            });
        }
        return ApiResponse.Ok("权限设置成功");
    }

    public async Task<ApiResponse> DeleteAttachmentPermissionAsync(long permissionId)
    {
        var perm = await _permRepo.GetByIdAsync(permissionId);
        if (perm == null) return ApiResponse.Fail("权限记录不存在");
        await _permRepo.DeleteAsync(perm);
        return ApiResponse.Ok("权限已删除");
    }

    public async Task<(bool canPreview, bool canDownload)> CheckAttachmentPermissionAsync(long attachmentId, long userId, long? departmentId, string role)
    {
        // Leaders and admins have full access
        if (role is "SuperAdmin" or "CompanyLeader" or "DepartmentLeader")
            return (true, true);

        // Check user-specific permission
        var userPerm = await _permRepo.Query()
            .Where(p => p.AttachmentId == attachmentId && p.UserId == userId)
            .FirstOrDefaultAsync();
        if (userPerm != null)
            return (userPerm.CanPreview, userPerm.CanDownload);

        // Check department permission
        if (departmentId.HasValue)
        {
            var deptPerm = await _permRepo.Query()
                .Where(p => p.AttachmentId == attachmentId && p.DepartmentId == departmentId.Value)
                .FirstOrDefaultAsync();
            if (deptPerm != null)
                return (deptPerm.CanPreview, deptPerm.CanDownload);
        }

        // Default: preview allowed, download not allowed
        return (true, false);
    }

    public async Task<ApiResponse> DeleteAttachmentAsync(long attachmentId)
    {
        var attachment = await _attachRepo.GetByIdAsync(attachmentId);
        if (attachment == null) return ApiResponse.Fail("附件不存在");
        await _fileStorage.DeleteFileAsync(attachment.StoragePath);
        await _attachRepo.DeleteAsync(attachment);
        return ApiResponse.Ok("附件已删除");
    }

    private static CommentDto MapComment(DocumentComment c) => new(
        c.Id, c.DocumentId, c.UserId, c.User?.DisplayName, c.Content, c.ParentId, c.MentionedUserIds, c.CreatedAt,
        c.Replies?.OrderBy(r => r.CreatedAt).Select(MapComment).ToList());
}
