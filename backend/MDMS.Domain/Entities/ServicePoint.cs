using MDMS.Domain.Common;
using MDMS.Domain.Enums;

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

    /// <summary>
    /// The Distribution Transformer node (<see cref="HierarchyNode"/>, <see cref="HierarchyNodeType.DistributionTransformer"/>)
    /// this service point draws supply from — the link that completes the electrical hierarchy
    /// down to the consumer for energy-audit aggregation. Null until assigned (e.g. at
    /// installation time); a consumer with no DT link is simply excluded from that DT's rollup.
    /// </summary>
    public Guid? DistributionTransformerNodeId { get; private set; }

    private ServicePoint() { }

    public ServicePoint(Guid customerId, string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));

        CustomerId = customerId;
        Address = address;
    }

    /// <summary>Links this service point to the DT node supplying it. Throws if <paramref name="dtNode"/> isn't a DT-level node.</summary>
    public void AssignDistributionTransformer(HierarchyNode dtNode)
    {
        ArgumentNullException.ThrowIfNull(dtNode);
        if (dtNode.NodeType != HierarchyNodeType.DistributionTransformer)
        {
            throw new ArgumentException(
                $"A service point must link to a {HierarchyNodeType.DistributionTransformer} node, not a {dtNode.NodeType}.",
                nameof(dtNode));
        }

        DistributionTransformerNodeId = dtNode.Id;
    }
}
