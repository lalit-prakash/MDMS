using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>
/// A physical point of supply/consumption belonging to a <see cref="Customer"/>. A meter is
/// installed at a service point over time (see <see cref="MeterAssignment"/>) — the service
/// point, not the meter, is the stable identity a customer's usage history is organized around.
/// </summary>
public class ServicePoint : Entity
{
    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }

    public string Address { get; private set; } = default!;

    private ServicePoint() { }

    public ServicePoint(Guid customerId, string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));

        CustomerId = customerId;
        Address = address;
    }
}
