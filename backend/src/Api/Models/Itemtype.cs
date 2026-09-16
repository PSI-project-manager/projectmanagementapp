namespace Api.Models;

public partial class Itemtype
{
    public int Itemtypeid { get; set; }

    public string Name { get; set; } = null!;

    public bool Isactive { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
