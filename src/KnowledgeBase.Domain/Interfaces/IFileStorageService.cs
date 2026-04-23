namespace KnowledgeBase.Domain.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream stream, string fileName, string folder, CancellationToken ct = default);
    Task<Stream?> GetFileAsync(string path, CancellationToken ct = default);
    Task DeleteFileAsync(string path, CancellationToken ct = default);
    string GetFileUrl(string path);
}
