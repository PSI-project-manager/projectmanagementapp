namespace Domain.Entities;

public sealed class Project
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;

    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Project(Guid organizationId, string name, string? description = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException(
                "A project must belong to an organization.",
                nameof(organizationId)
            );

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        Update(name, description);
    }

    public void Update(string name, string? description)
    {
        SetName(name);
        Description = NormalizeDescription(description);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException(
                $"Project name cannot exceed {MaxNameLength} characters.",
                nameof(name)
            );

        Name = trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var trimmed = description.Trim();
        if (trimmed.Length > MaxDescriptionLength)
            throw new ArgumentException(
                $"Project description cannot exceed {MaxDescriptionLength} characters.",
                nameof(description)
            );

        return trimmed;
    }
}
