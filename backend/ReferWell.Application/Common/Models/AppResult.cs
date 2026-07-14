namespace ReferWell.Application.Common.Models;

public enum AppStatus { Ok, Created, BadRequest, NotFound, Forbid, Conflict, Unauthorized, File, ServerError }

public sealed class AppResult
{
    public AppStatus Status { get; init; }
    public object? Value { get; init; }
    public string? Message { get; init; }
    // For file downloads:
    public byte[]? FileBytes { get; init; }
    public string? ContentType { get; init; }
    public string? FileDownloadName { get; init; }
    public bool InlineDisposition { get; init; }

    public static AppResult Success(object? value = null) => new() { Status = AppStatus.Ok, Value = value };
    public static AppResult Created(object? value = null) => new() { Status = AppStatus.Created, Value = value };
    public static AppResult BadRequest(string message) => new() { Status = AppStatus.BadRequest, Message = message, Value = new { message } };
    public static AppResult NotFound(string? message = null) => new() { Status = AppStatus.NotFound, Message = message, Value = message == null ? null : new { message } };
    public static AppResult Forbid() => new() { Status = AppStatus.Forbid };
    public static AppResult Conflict(string message) => new() { Status = AppStatus.Conflict, Message = message, Value = new { message } };
    public static AppResult Unauthorized(string message) => new() { Status = AppStatus.Unauthorized, Message = message, Value = new { message } };
    public static AppResult ServerError(object value) => new() { Status = AppStatus.ServerError, Value = value };
    public static AppResult File(byte[] bytes, string contentType, string? downloadName = null, bool inline = false) => new() { Status = AppStatus.File, FileBytes = bytes, ContentType = contentType, FileDownloadName = downloadName, InlineDisposition = inline };
}
