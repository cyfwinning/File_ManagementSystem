using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class Space : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public SpaceType Type { get; set; }
    public SpaceVisibility Visibility { get; set; } = SpaceVisibility.Internal;
    public string? EncryptionKey { get; set; }
    public string? Template { get; set; }
    public long OwnerId { get; set; }
    public User? Owner { get; set; }
    public long? DepartmentId { get; set; }
    public int SortOrder { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<SpacePermission> Permissions { get; set; } = new List<SpacePermission>();
}
