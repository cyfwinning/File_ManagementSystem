using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? ParentId { get; set; }
    public Department? Parent { get; set; }
    public int SortOrder { get; set; }

    public ICollection<Department> Children { get; set; } = new List<Department>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
