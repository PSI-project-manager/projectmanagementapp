using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Entities;

public partial class Itemhistory
{
    public int Itemhistoryid { get; set; }

    public int Itemid { get; set; }

    public int Changedbyuserid { get; set; }

    public string Fieldchanged { get; set; } = null!;

    public string? Oldvalue { get; set; }

    public string? Newvalue { get; set; }

    public DateTime Changedat { get; set; }

    public virtual User Changedbyuser { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;
}
