using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class Recommendation : BaseEntity
{
    public long DocumentId { get; set; }
    public Document? Document { get; set; }
    public long RecommenderId { get; set; }
    public User? Recommender { get; set; }
    public long? TargetUserId { get; set; }
    public User? TargetUser { get; set; }
    public long? TargetDepartmentId { get; set; }
    public Department? TargetDepartment { get; set; }
    public bool IsCompanyWide { get; set; }
    public string? Description { get; set; }
    public bool IsMandatory { get; set; }
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Medium;
    public DateTime? Deadline { get; set; }
    public bool IsActive { get; set; } = true;
}
