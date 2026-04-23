using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Domain.Entities;

public class Favorite : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public long DocumentId { get; set; }
    public Document? Document { get; set; }
}
