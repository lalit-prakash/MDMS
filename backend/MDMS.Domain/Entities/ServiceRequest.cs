using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>A consumer service request (meter replacement/shifting, load change, name/contact/
/// address correction, tariff conversion, etc.) — deliberately separate from
/// <see cref="Complaint"/>, per the reference spec's own module split.</summary>
public class ServiceRequest : Entity
{
    public string RequestNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public ServiceRequestCategory Category { get; private set; }
    public string Description { get; private set; } = default!;
    public ServiceRequestStatus Status { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private ServiceRequest() { }

    public ServiceRequest(Guid customerId, ServiceRequestCategory category, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        CustomerId = customerId;
        Category = category;
        Description = description;
        Status = ServiceRequestStatus.Open;
        RequestNumber = $"SR-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
    }

    public void StartProgress()
    {
        if (Status != ServiceRequestStatus.Open)
            throw new InvalidOperationException($"Only an Open request can start progress (was {Status}).");
        Status = ServiceRequestStatus.InProgress;
    }

    public void Complete(string resolutionNote)
    {
        if (Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Rejected or ServiceRequestStatus.Cancelled)
            throw new InvalidOperationException($"A {Status} request cannot be completed.");
        ResolutionNote = resolutionNote;
        CompletedAtUtc = DateTime.UtcNow;
        Status = ServiceRequestStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Rejected or ServiceRequestStatus.Cancelled)
            throw new InvalidOperationException($"A {Status} request cannot be cancelled.");
        Status = ServiceRequestStatus.Cancelled;
    }
}
