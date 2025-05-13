using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class Productsale
{
    public int Id { get; set; }

    public int Agentid { get; set; }

    public int Productid { get; set; }

    public DateOnly Saledate { get; set; }

    public int Productcount { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
