using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public UserRole Role { get; set; } = UserRole.Editor;
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }

    public ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();
    public ICollection<LearningRecord> LearningRecords { get; set; } = new List<LearningRecord>();
    public ICollection<Recommendation> ReceivedRecommendations { get; set; } = new List<Recommendation>();
}
