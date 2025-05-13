using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class Agent
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public int Agenttypeid { get; set; }

    public string? Address { get; set; }

    public string Inn { get; set; } = null!;

    public string? Kpp { get; set; }

    public string? Directorname { get; set; }

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? Logo { get; set; }

    public int Priority { get; set; }

    public virtual Agenttype Agenttype { get; set; } = null!;

    public virtual ICollection<Productsale> Productsales { get; set; } = new List<Productsale>();
}
