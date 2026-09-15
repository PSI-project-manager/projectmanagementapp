using Domain.Entities;
using Xunit;

namespace Domain.Tests.Entities;

public class ProjectTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesProject()
    {
        var organizationId = Guid.NewGuid();

        var project = new Project(organizationId, "Website Revamp", "Redesign the marketing site");

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal(organizationId, project.OrganizationId);
        Assert.Equal("Website Revamp", project.Name);
        Assert.Equal("Redesign the marketing site", project.Description);
    }

    [Fact]
    public void Constructor_TrimsNameAndDescription()
    {
        var project = new Project(Guid.NewGuid(), "  Mobile App  ", "  v1 launch  ");

        Assert.Equal("Mobile App", project.Name);
        Assert.Equal("v1 launch", project.Description);
    }

    [Fact]
    public void Constructor_WithoutDescription_LeavesDescriptionNull()
    {
        var project = new Project(Guid.NewGuid(), "Solo Project");

        Assert.Null(project.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => new Project(Guid.NewGuid(), name, null));
    }

    [Fact]
    public void Constructor_WithEmptyOrganizationId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Project(Guid.Empty, "Name", null));
    }

    [Fact]
    public void Constructor_WithNameOverMaxLength_Throws()
    {
        var tooLong = new string('a', Project.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => new Project(Guid.NewGuid(), tooLong, null));
    }

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var project = new Project(Guid.NewGuid(), "Old Name", "Old description");

        project.Update("New Name", "New description");

        Assert.Equal("New Name", project.Name);
        Assert.Equal("New description", project.Description);
    }

    [Fact]
    public void Update_WithBlankDescription_ClearsDescription()
    {
        var project = new Project(Guid.NewGuid(), "Name", "Has a description");

        project.Update("Name", "   ");

        Assert.Null(project.Description);
    }

    [Fact]
    public void Update_WithBlankName_Throws()
    {
        var project = new Project(Guid.NewGuid(), "Name", null);

        Assert.Throws<ArgumentException>(() => project.Update("", null));
    }
}
