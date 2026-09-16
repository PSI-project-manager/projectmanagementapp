using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class Status
{
    public int Statusid { get; set; }

    public string Name { get; set; } = null!;

    public int Sortorder { get; set; }

    public bool Isactive { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
