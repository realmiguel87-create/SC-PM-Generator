using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SCPM.Domain.Entities;
using SCPM.Infrastructure.Persistence;

namespace SCPM.IntegrationTests;

/// <summary>Creates a Security.User with a single role and returns its Entra object id, ready to
/// hand to TestAuthHandler via the X-Test-Entra-Oid header.</summary>
public static class TestUserSeeder
{
    public static async Task<string> CreateUserWithRoleAsync(ScpmWebApplicationFactory factory, string roleName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entraOid = Guid.NewGuid().ToString();
        var role = await db.Roles.SingleAsync(r => r.Name == roleName);

        var user = new User
        {
            EntraObjectId = entraOid,
            DisplayName = $"Test {roleName}",
            Email = $"{Guid.NewGuid():N}@test.stirling.gov.uk",
            IsActive = true,
            CreatedBy = Guid.Empty
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = role.Id, CreatedBy = Guid.Empty });
        await db.SaveChangesAsync();

        return entraOid;
    }

    /// <summary>An Entra object id with no matching Security.User row — simulates someone Entra
    /// ID has authenticated but the platform hasn't provisioned, who should get no role claims
    /// and therefore fail every RequireRole() policy.</summary>
    public static string UnprovisionedEntraOid() => Guid.NewGuid().ToString();
}
