namespace Api.Models;

public partial class Projectuser
{
    public int Projectid { get; set; }

    public int Userid { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}