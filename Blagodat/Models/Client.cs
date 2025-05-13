using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class Client
{
    // Основные свойства, которые мапятся на колонки в базе данных
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Email { get; set; } = null!;
    public string Address { get; set; } = null!;
    public DateTime BirthDate { get; set; }
    public string PassportData { get; set; } = null!;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = null!;
    
    // Свойство, которое мапится на колонку full_name в базе данных
    public string FullName { get; set; } = string.Empty;
    
    // Вычисляемые свойства, не мапящиеся на базу данных
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string FirstName 
    { 
        get => FullName.Split(' ').Length > 1 ? FullName.Split(' ')[1] : string.Empty; 
        set 
        {
            var parts = FullName.Split(' ');
            if (parts.Length > 1)
            {
                FullName = $"{parts[0]} {value}";
            }
            else
            {
                FullName = $"{FullName} {value}".Trim();
            }
        }
    }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string LastName 
    { 
        get => FullName.Split(' ').Length > 0 ? FullName.Split(' ')[0] : FullName; 
        set 
        {
            var parts = FullName.Split(' ');
            if (parts.Length > 1)
            {
                FullName = $"{value} {parts[1]}";
            }
            else
            {
                FullName = value;
            }
        }
    }
    
    // Свойство-алиас для совместимости со старым кодом
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Passport { get => PassportData; set => PassportData = value; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
