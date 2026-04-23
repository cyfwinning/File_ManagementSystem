using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Domain.Entities;

public class DocumentVersion : BaseEntity
{
    public long DocumentId { get; set; }
    public Document? Document { get; set; }
    public int VersionNumber { get; set; }
    public string? Content { get; set; }
    public string? ChangeNote { get; set; }
    public long EditorId { get; set; }
    public User? Editor { get; set; }
}
