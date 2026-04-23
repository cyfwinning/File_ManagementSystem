using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class Attachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? PreviewPath { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public AttachmentType Type { get; set; }
    public long DocumentId { get; set; }
    public Document? Document { get; set; }
    public long UploaderId { get; set; }
    public User? Uploader { get; set; }
    public string? FileHash { get; set; }
}
