using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// A billing-category reference (Domestic, Industrial, Commercial, ...) used to classify a
/// <see cref="Customer"/>/<see cref="ServicePoint"/> for downstream TOD-slab mapping and bill
/// determinant generation. This is master-data classification only — MDMS does not compute
/// tariff rates, slabs, or charges itself; that calculation is entirely a separate downstream
/// system's responsibility. Duplicating rate logic here would create two sources of truth for
/// the same billing rule.
/// </summary>
public class TariffCategory : Entity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private TariffCategory() { }

    public TariffCategory(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Code = code;
        Name = name;
        Description = description;
    }

    public void Rename(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name;
        Description = description;
    }
}
