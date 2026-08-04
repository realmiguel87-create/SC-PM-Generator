using FluentAssertions;
using SCPM.Application.Projects.Commands.CreateProject;
using Xunit;

namespace SCPM.UnitTests.Projects;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes_validation()
    {
        var command = new CreateProjectCommand(
            "PRJ-0001", "Stirling Community Campus", "New build campus", null, 25_000_000m,
            new DateOnly(2026, 1, 1), new DateOnly(2029, 1, 1), null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_project_ref_fails_validation()
    {
        var command = new CreateProjectCommand(
            "", "Stirling Community Campus", null, null, 1_000_000m, null, null, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProjectCommand.ProjectRef));
    }

    [Fact]
    public void Target_completion_before_start_date_fails_validation()
    {
        var command = new CreateProjectCommand(
            "PRJ-0002", "Bridge Refurbishment", null, null, 500_000m,
            new DateOnly(2027, 1, 1), new DateOnly(2026, 1, 1), null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
