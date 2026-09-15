using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Entities;

public partial class Role
{
    public int Roleid { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Userrole> Userroles { get; set; } = new List<Userrole>();
}
