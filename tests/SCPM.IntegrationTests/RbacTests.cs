using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace SCPM.IntegrationTests;

/// <summary>
/// Exercises RequireRole()/[Authorize(Policy=...)] end to end through the real ASP.NET Core
/// authorization pipeline (TestAuthHandler supplies only an identity; EntraClaimsTransformation
/// resolves it to real roles from the database, exactly as it would for a real Entra ID token).
///
/// This is the suite that would have caught (and did catch, during Phase 7) the seeded
/// Role.Name values not matching RequireRole()'s PascalCase-no-space policy names for every
/// multi-word role — a bug that made "CanWrite"/"CanApprove" silently reject every role except
/// Administrator and Director, undetected through six prior phases because no test exercised
/// real role-string matching.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class RbacTests : IAsyncLifetime
{
    private readonly ScpmWebApplicationFactory _factory;

    public RbacTests(ScpmWebApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient ClientAs(string? entraOid)
    {
        var client = _factory.CreateClient();
        if (entraOid is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.EntraOidHeader, entraOid);
        return client;
    }

    private static object ValidProjectPayload(string projectRef) => new
    {
        ProjectRef = projectRef,
        Name = "RBAC Test Project",
        Description = (string?)null,
        ProgrammeId = (Guid?)null,
        ApprovedBudget = 1_000_000m,
        StartDate = (DateOnly?)null,
        TargetCompletionDate = (DateOnly?)null,
        SponsorUserId = (Guid?)null,
        ProjectManagerUserId = (Guid?)null,
    };

    [Fact]
    public async Task Unauthenticated_request_is_rejected_with_401()
    {
        var client = ClientAs(entraOid: null);

        var response = await client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Entra_authenticated_but_unprovisioned_user_is_rejected_with_403_on_a_write_endpoint()
    {
        var client = ClientAs(TestUserSeeder.UnprovisionedEntraOid());

        var response = await client.PostAsJsonAsync("/api/projects", ValidProjectPayload("RBAC-UNPROV"));

        // Authenticated (TestAuthHandler succeeds), but no Security.User row means no role
        // claims, so RequireRole("CanWrite") correctly denies with 403, not 401.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("ProjectManager")]      // multi-word role — this is the one the Name/DisplayName bug broke
    [InlineData("CommercialManager")]
    [InlineData("QuantitySurveyor")]
    [InlineData("Administrator")]       // single-word role — worked even before the fix
    public async Task Roles_in_the_CanWrite_policy_can_create_a_project(string roleName)
    {
        var entraOid = await TestUserSeeder.CreateUserWithRoleAsync(_factory, roleName);
        var client = ClientAs(entraOid);

        var response = await client.PostAsJsonAsync("/api/projects", ValidProjectPayload($"RBAC-{roleName[..Math.Min(roleName.Length, 12)]}"));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"{roleName} is listed in the CanWrite policy in Program.cs. Body: {body}");
    }

    [Fact]
    public async Task ReadOnlyUser_cannot_create_a_project()
    {
        var entraOid = await TestUserSeeder.CreateUserWithRoleAsync(_factory, "ReadOnlyUser");
        var client = ClientAs(entraOid);

        var response = await client.PostAsJsonAsync("/api/projects", ValidProjectPayload("RBAC-READONLY"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            because: "ReadOnlyUser is deliberately excluded from the CanWrite policy");
    }

    [Fact]
    public async Task ReadOnlyUser_can_still_read_the_project_list()
    {
        var entraOid = await TestUserSeeder.CreateUserWithRoleAsync(_factory, "ReadOnlyUser");
        var client = ClientAs(entraOid);

        var response = await client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
