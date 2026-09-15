using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Entities;

public partial class User
{
    public int Userid { get; set; }

    public string Email { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public string Fullname { get; set; } = null!;

    public bool Isactive { get; set; }

    public DateTime Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public virtual ICollection<Item> ItemAssignedtousers { get; set; } = new List<Item>();

    public virtual ICollection<Item> ItemCreatedbyusers { get; set; } = new List<Item>();

    public virtual ICollection<Itemhistory> Itemhistories { get; set; } = new List<Itemhistory>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Projectuser> Projectusers { get; set; } = new List<Projectuser>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<Userrole> Userroles { get; set; } = new List<Userrole>();
}
