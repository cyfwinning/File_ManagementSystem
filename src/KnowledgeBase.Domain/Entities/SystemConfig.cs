using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Domain.Entities;

public class SystemConfig : BaseEntity
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
}
