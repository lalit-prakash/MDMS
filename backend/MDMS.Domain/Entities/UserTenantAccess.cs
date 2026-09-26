using MDMS.Domain.Common;

namespace MDMS.Domain.Entities;

/// <summary>Grants a user access to an organisation (tenant) other than their home one, so the
/// header Organisation switcher can offer it. A user always has access to their own
/// <see cref="Entity.TenantId"/> without needing a row here.</summary>
public class UserTenantAccess : Entity, ITenantExempt
{
    public Guid UserId { get; private set; }
    public Guid GrantedTenantId { get; private set; }

    private UserTenantAccess() { }

    public UserTenantAccess(Guid userId, Guid grantedTenantId)
    {
        UserId = userId;
        GrantedTenantId = grantedTenantId;
    }
}
