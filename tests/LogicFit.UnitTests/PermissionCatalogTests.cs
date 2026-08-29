using LogicFit.Domain.Constants;

namespace LogicFit.UnitTests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void ApprovedCatalogContainsTheExpectedShape()
    {
        Assert.Equal(16, PermissionCatalog.Permissions.Count);
        Assert.Equal(3, PermissionCatalog.Roles.Count);
        Assert.Equal(15, PermissionCatalog.RolePermissions.Count);
        Assert.Equal(PermissionCatalog.Permissions.Count, PermissionCatalog.Permissions.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(PermissionCatalog.Roles.Count, PermissionCatalog.Roles.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(PermissionCatalog.RolePermissions.Count, PermissionCatalog.RolePermissions.Distinct().Count());
    }

    [Fact]
    public void ApprovalPermissionsAreNotPresentInTheFoundationCatalog()
    {
        Assert.DoesNotContain(PermissionCatalog.Permissions, permission => permission.Key.StartsWith("training.", StringComparison.Ordinal));
        Assert.DoesNotContain(PermissionCatalog.Permissions, permission => permission.Key.StartsWith("nutrition.", StringComparison.Ordinal));
    }
}
