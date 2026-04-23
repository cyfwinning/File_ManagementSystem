using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Domain.Entities;

public class OperationLog : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public long? TargetId { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
