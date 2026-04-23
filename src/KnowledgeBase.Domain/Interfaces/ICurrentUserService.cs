namespace KnowledgeBase.Domain.Interfaces;

public interface ICurrentUserService
{
    long? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    string? IpAddress { get; }
}
