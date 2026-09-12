namespace MDMS.Domain.Common;

/// <summary>Base type for every MDMS domain entity: a GUID identity plus creation bookkeeping.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>UTC timestamp the row was first persisted. Never mutated after insert.</summary>
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
}
