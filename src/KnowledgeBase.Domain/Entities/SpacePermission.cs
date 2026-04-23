using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class SpacePermission : BaseEntity
{
    public long SpaceId { get; set; }
    public Space? Space { get; set; }
    public long? UserId { get; set; }
    public User? User { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public UserRole? RoleFilter { get; set; }
    public string AllowedActions { get; set; } = string.Empty; // JSON array of PermissionAction
}
