using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// A named set of filter values a user saved on one screen (e.g. "meter-data:ls"), to re-apply
/// later. Private to its owner. The values are stored as an opaque JSON object of string
/// key/values — exactly the query parameters that screen applies — so the backend never needs to
/// know each screen's filter schema.
/// </summary>
public class SavedFilter : Entity
{
    public Guid UserId { get; private set; }
    public string Screen { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string FilterJson { get; private set; } = default!;

    private SavedFilter() { }

    public SavedFilter(Guid userId, string screen, string name, string filterJson)
    {
        if (string.IsNullOrWhiteSpace(screen)) throw new ArgumentException("Screen is required.", nameof(screen));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(filterJson)) throw new ArgumentException("Filter values are required.", nameof(filterJson));

        UserId = userId;
        Screen = screen.Trim();
        Name = name.Trim();
        FilterJson = filterJson;
    }
}
