using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blagodat.Models;
public partial class User
{
    public string Username { get => Login; set => Login = value; }
    public int UserId { get; set; }
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = null!;
    
    [NotMapped] 
    public int? EmployeeId { get; set; }
    public string Role { get; set; } = null!;
    public string? PhotoPath { get; set; }
    public virtual ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
}
