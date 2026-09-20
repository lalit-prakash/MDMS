using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// An asynchronous "Download Request": instead of streaming a large export inside the HTTP
/// request that asked for it, the user files a request (title + the list endpoint path/query they
/// were viewing), a background worker produces the CSV, and the user collects it from the
/// Download Requests screen when it is ready. Private to the requesting user.
/// </summary>
public class DownloadRequest : Entity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = default!;

    /// <summary>The API path + query of the list screen being exported (no page/export params).</summary>
    public string RequestPath { get; private set; } = default!;

    public DownloadRequestStatus Status { get; private set; } = DownloadRequestStatus.Pending;
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FileName { get; private set; }
    public byte[]? Content { get; private set; }
    public long? SizeBytes { get; private set; }
    public int? RowCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    private DownloadRequest() { }

    public DownloadRequest(Guid userId, string title, string requestPath)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(requestPath)) throw new ArgumentException("Request path is required.", nameof(requestPath));
        UserId = userId;
        Title = title.Trim();
        RequestPath = requestPath;
    }

    public void MarkProcessing()
    {
        if (Status != DownloadRequestStatus.Pending)
            throw new InvalidOperationException($"Only a Pending request can start processing (was {Status}).");
        Status = DownloadRequestStatus.Processing;
    }

    public void Complete(string fileName, byte[] content, int rowCount)
    {
        if (Status != DownloadRequestStatus.Processing)
            throw new InvalidOperationException($"Only a Processing request can complete (was {Status}).");
        Status = DownloadRequestStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        FileName = fileName;
        Content = content;
        SizeBytes = content.LongLength;
        RowCount = rowCount;
    }

    public void Fail(string message)
    {
        if (Status is DownloadRequestStatus.Completed or DownloadRequestStatus.Failed)
            throw new InvalidOperationException($"A {Status} request cannot fail.");
        Status = DownloadRequestStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = message.Length > 500 ? message[..500] : message;
    }
}
