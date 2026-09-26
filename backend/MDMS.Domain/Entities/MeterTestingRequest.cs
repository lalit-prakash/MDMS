using MDMS.Domain.Common;
using MDMS.Domain.Enums;

namespace MDMS.Domain.Entities;

/// <summary>
/// A consumer's request for official testing of their electricity meter — deliberately its own
/// entity, not folded into <see cref="Complaint"/>, per the reference spec's requirement that
/// Meter Testing be a dedicated module. <see cref="RequestNumber"/> is the consumer-facing
/// reference (e.g. MTRT-20260926-00125); test-result fields are only meaningful once Completed.
/// </summary>
public class MeterTestingRequest : Entity
{
    public string RequestNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public Guid MeterId { get; private set; }
    public MeterTestingReason Reason { get; private set; }
    public string? ConsumerRemarks { get; private set; }
    public MeterTestingStatus Status { get; private set; }

    public DateTime? ScheduledAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? TestResult { get; private set; }
    public string? AccuracyResult { get; private set; }
    public string? FinalRemarks { get; private set; }

    private MeterTestingRequest() { }

    public MeterTestingRequest(Guid customerId, Guid meterId, MeterTestingReason reason, string? consumerRemarks)
    {
        CustomerId = customerId;
        MeterId = meterId;
        Reason = reason;
        ConsumerRemarks = consumerRemarks;
        Status = MeterTestingStatus.Submitted;
        RequestNumber = $"MTRT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
    }

    public void Schedule(DateTime scheduledAtUtc)
    {
        if (Status != MeterTestingStatus.Submitted)
            throw new InvalidOperationException($"Only a Submitted request can be scheduled (was {Status}).");
        ScheduledAtUtc = scheduledAtUtc;
        Status = MeterTestingStatus.Scheduled;
    }

    public void Complete(string testResult, string accuracyResult, string? finalRemarks)
    {
        if (Status != MeterTestingStatus.Scheduled)
            throw new InvalidOperationException($"Only a Scheduled request can be completed (was {Status}).");
        if (string.IsNullOrWhiteSpace(testResult))
            throw new ArgumentException("Test result is required.", nameof(testResult));

        TestResult = testResult;
        AccuracyResult = accuracyResult;
        FinalRemarks = finalRemarks;
        CompletedAtUtc = DateTime.UtcNow;
        Status = MeterTestingStatus.Completed;
    }

    public void Reject(string reason)
    {
        if (Status is MeterTestingStatus.Completed or MeterTestingStatus.Rejected or MeterTestingStatus.Cancelled)
            throw new InvalidOperationException($"A {Status} request cannot be rejected.");
        FinalRemarks = reason;
        Status = MeterTestingStatus.Rejected;
    }

    public void Cancel()
    {
        if (Status is MeterTestingStatus.Completed or MeterTestingStatus.Rejected or MeterTestingStatus.Cancelled)
            throw new InvalidOperationException($"A {Status} request cannot be cancelled.");
        Status = MeterTestingStatus.Cancelled;
    }
}
