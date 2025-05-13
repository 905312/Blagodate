using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class Employee
{
    public string Photo { get; set; } = string.Empty;
    public string Firstname { get => FullName.Split(' ').Length > 1 ? FullName.Split(' ')[1] : FullName; }
    public string Lastname { get => FullName.Split(' ').Length > 0 ? FullName.Split(' ')[0] : FullName; }
    public int EmployeeId { get; set; }

    public string Code { get; set; } = null!;

    public string Position { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime? LastLogin { get; set; }

    public string? LoginType { get; set; }
}
