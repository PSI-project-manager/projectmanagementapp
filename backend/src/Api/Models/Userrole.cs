namespace Api.Models;

public partial class Userrole
{
    public int Userid { get; set; }

    public int Roleid { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
