using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Domain.Entities;

public class SearchHistory : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public int ResultCount { get; set; }
}
