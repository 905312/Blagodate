using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class Agenttype
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Image { get; set; }

    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
