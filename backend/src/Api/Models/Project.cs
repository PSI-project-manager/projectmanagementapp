namespace Api.Models;

public partial class Project
{
    public const int MaxNameLength = 150;
    public const int MaxDescriptionLength = 1000;

    public int Projectid { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool Isactive { get; set; } = true;

    public int Createdbyuserid { get; set; }

    public virtual User Createdbyuser { get; set; } = null!;

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
