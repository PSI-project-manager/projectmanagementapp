namespace Api.Models;

public partial class Item
{
    public int Itemid { get; set; }

    public int Projectid { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int Itemtypeid { get; set; }

    public int Statusid { get; set; }

    public int? Assignedtouserid { get; set; }

    public virtual User? Assignedtouser { get; set; }

    public virtual Itemtype Itemtype { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;
}
