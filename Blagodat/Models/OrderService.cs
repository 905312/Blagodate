using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class OrderService
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ServiceId { get; set; }

    public int Hours { get; set; }

    public decimal Cost { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalCost
    {
        get
        {
            if (Service != null && Cost <= 0)
                return Service.CostPerHour * Hours;
            return Cost;
        }
    }

    public virtual Order Order { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
