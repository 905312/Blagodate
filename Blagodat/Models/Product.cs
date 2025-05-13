using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public int? Producttypeid { get; set; }

    public string Articlenumber { get; set; } = null!;

    public string? Description { get; set; }

    public string? Image { get; set; }

    public int? Productionpersoncount { get; set; }

    public int? Productionworkshopnumber { get; set; }

    public decimal Mincostforagent { get; set; }

    public virtual ICollection<Productsale> Productsales { get; set; } = new List<Productsale>();

    public virtual Producttype? Producttype { get; set; }
}
