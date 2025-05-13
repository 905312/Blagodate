using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class Service
{
   
    public int ServiceId { get; set; }
    
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Id { get => ServiceId; set => ServiceId = value; }
    
    

    public string Code { get; set; } = string.Empty;
    public decimal CostPerHour { get; set; }
    public string Name { get; set; } = null!;
    
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal Cost { get => CostPerHour; set => CostPerHour = value; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Title { get => Name; set => Name = value; }

    public virtual ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
}
