namespace Api.Models;

public partial class Status
{
    public const int MaxNameLength = 100;

    public int Statusid { get; set; }

    public string Name { get; set; } = null!;

    public int Sortorder { get; set; }

    public bool Isactive { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
