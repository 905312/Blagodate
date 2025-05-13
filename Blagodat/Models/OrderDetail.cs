using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class OrderDetail
{
    public int? OrderId { get; set; }

    public string? OrderCode { get; set; }

    public DateOnly? CreationDate { get; set; }

    public TimeOnly? OrderTime { get; set; }

    public string? ClientCode { get; set; }

    public string? ClientName { get; set; }

    public string? ServiceNames { get; set; }

    public decimal? TotalCost { get; set; }

    public string? Status { get; set; }

    public DateOnly? ClosingDate { get; set; }

    public string? RentalTime { get; set; }
}
