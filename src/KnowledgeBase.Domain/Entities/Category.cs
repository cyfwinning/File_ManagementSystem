using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long SpaceId { get; set; }
    public Space? Space { get; set; }
    public long? ParentId { get; set; }
    public Category? Parent { get; set; }
    public int Level { get; set; } = 1; // Max 3 levels
    public int SortOrder { get; set; }

    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
