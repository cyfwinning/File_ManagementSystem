namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// Scoped service that holds the database type for the current request.
/// Set by middleware before DbContext resolution.
/// </summary>
public class DatabaseSession
{
    public string DatabaseType { get; set; } = "SQLite";
}
