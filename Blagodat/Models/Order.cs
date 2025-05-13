using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blagodat.Models;

using System.Linq;

public partial class Order
{
    public string OrderCode { get; set; } = string.Empty;
    public DateTime CreationDate { get => OrderDate; set => OrderDate = value; }
    public string OrderTime { get; set; } = string.Empty;
    public string ClientCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    public Client ClientCodeNavigation { get; set; } = null!;
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ServicesList 
    {
        get
        {
            if (OrderServices == null || !OrderServices.Any())
                return string.Empty;
                
            return string.Join(", ", OrderServices
                .Where(os => os.Service != null)
                .Select(os => os.Service.Name));
        }
    }
    public int Id { get; set; }
    public int OrderId { get => Id; set => Id = value; }

    public DateTime OrderDate { get; set; }

    public int ClientId { get; set; }

    public decimal TotalCost { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();

    public DateOnly? ClosingDate { get; set; }

    public string RentalTime { get; set; } = null!;
}
