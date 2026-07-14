using ReferWell.Application.Common.Interfaces;

namespace ReferWell.Infrastructure.Services;

public class AttachmentStorage : IAttachmentStorage
{
    private static string UploadsFolder => Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    public async Task<(string relativePath, string storedFileName)> SaveAsync(
        Guid fileId,
        string originalFileName,
        Stream content,
        CancellationToken ct = default)
    {
        var uploadsFolder = UploadsFolder;
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{fileId}{extension}";
        var filePath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await content.CopyToAsync(stream, ct);
        }

        return ($"/uploads/{storedFileName}", storedFileName);
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        var filePath = ResolvePath(relativePath);
        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllBytesAsync(filePath, ct);
    }

    public bool Exists(string relativePath)
    {
        var filePath = ResolvePath(relativePath);
        return File.Exists(filePath);
    }

    private static string ResolvePath(string relativePath) =>
        Path.Combine(UploadsFolder, Path.GetFileName(relativePath));
}
