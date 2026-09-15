using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class Project
{
    public int Projectid { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool Isactive { get; set; }

    public DateTime Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public int Createdbyuserid { get; set; }

    public virtual User Createdbyuser { get; set; } = null!;

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<Projectuser> Projectusers { get; set; } = new List<Projectuser>();
}
