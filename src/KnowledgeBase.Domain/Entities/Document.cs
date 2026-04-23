using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? CoverUrl { get; set; }
    public string? Content { get; set; }
    public string? Summary { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public DocumentEditMode EditMode { get; set; } = DocumentEditMode.RichText;
    public long CategoryId { get; set; }
    public Category? Category { get; set; }
    public long SpaceId { get; set; }
    public Space? Space { get; set; }
    public long AuthorId { get; set; }
    public User? Author { get; set; }
    public string? Tags { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public int ViewCount { get; set; }
    public int FavoriteCount { get; set; }
    public int EditCount { get; set; }
    public int SortOrder { get; set; }
    public int CurrentVersion { get; set; } = 1;

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<DocumentComment> Comments { get; set; } = new List<DocumentComment>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<LearningRecord> LearningRecords { get; set; } = new List<LearningRecord>();
}
