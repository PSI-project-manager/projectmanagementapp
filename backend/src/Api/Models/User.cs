namespace Api.Models;

public partial class User
{
    public const int MaxEmailLength = 255;
    public const int MaxFullNameLength = 150;
    public const int MinPasswordLength = 8;

    public int Userid { get; set; }

    public string Email { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public string Fullname { get; set; } = null!;

    public bool Isactive { get; set; }

    public virtual ICollection<Item> ItemAssignedtousers { get; set; } = new List<Item>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Userrole> Userroles { get; set; } = new List<Userrole>();

    /// <summary>Projects this user has been explicitly granted access to (US-06).</summary>
    public virtual ICollection<Projectuser> Projectusers { get; set; } = new List<Projectuser>();
}