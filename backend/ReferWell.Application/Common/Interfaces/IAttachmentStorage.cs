namespace ReferWell.Application.Common.Interfaces;

public interface IAttachmentStorage
{
    Task<(string relativePath, string storedFileName)> SaveAsync(Guid fileId, string originalFileName, Stream content, CancellationToken ct = default);
    Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct = default);
    bool Exists(string relativePath);
}
