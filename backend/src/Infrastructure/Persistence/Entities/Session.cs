using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Entities;

public partial class Session
{
    public Guid Sessionid { get; set; }

    public int Userid { get; set; }

    public string Token { get; set; } = null!;

    public DateTime Createdat { get; set; }

    public DateTime Expiresat { get; set; }

    public DateTime? Revokedat { get; set; }

    public virtual User User { get; set; } = null!;
}
