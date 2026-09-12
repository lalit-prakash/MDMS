using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// An active hold blocking further measurement processing for one meter after a data-quality
/// event (e.g. negative consumption) is detected. This is about measurement processing/VEE, not
/// billing — any downstream consumer of this data should treat a meter with an active hold as
/// having no trustworthy new usage data yet.
/// </summary>
public class DataQualityHold : Entity
{
    public Guid MeterId { get; private set; }
    public string Reason { get; private set; } = default!;
    public DateTime RaisedAtUtc { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime? ClearedAtUtc { get; private set; }
    public string? ResolutionNote { get; private set; }

    private DataQualityHold() { }

    public static DataQualityHold Raise(Guid meterId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A hold must record why it was raised.", nameof(reason));

        return new DataQualityHold
        {
            MeterId = meterId,
            Reason = reason,
            RaisedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>Reactivates an already-existing hold for this meter rather than creating a duplicate row.</summary>
    public void Reactivate(string reason)
    {
        Reason = reason;
        RaisedAtUtc = DateTime.UtcNow;
        IsActive = true;
        ClearedAtUtc = null;
        ResolutionNote = null;
    }

    public void Clear(string resolutionNote)
    {
        if (string.IsNullOrWhiteSpace(resolutionNote))
            throw new ArgumentException("Clearing a hold requires a resolution note.", nameof(resolutionNote));
        if (!IsActive)
            throw new InvalidOperationException("Hold is not active.");

        IsActive = false;
        ClearedAtUtc = DateTime.UtcNow;
        ResolutionNote = resolutionNote;
    }
}
