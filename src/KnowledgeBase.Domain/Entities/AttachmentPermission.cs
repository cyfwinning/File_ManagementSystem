using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class AttachmentPermission : BaseEntity
{
    public long AttachmentId { get; set; }
    public Attachment? Attachment { get; set; }
    public long? UserId { get; set; }
    public User? User { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool CanPreview { get; set; } = true;
    public bool CanDownload { get; set; }
    public long GrantedById { get; set; }
    public User? GrantedBy { get; set; }
}
