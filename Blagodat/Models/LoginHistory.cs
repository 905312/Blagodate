using System;
using System.Collections.Generic;

namespace Blagodat.Models;

public partial class LoginHistory
{
    public DateTime LoginTime { get => LoginDateTime; set => LoginDateTime = value; }
    public bool Success { get => IsSuccessful; set => IsSuccessful = value; }
    public string UserLogin { get => Username; set => Username = value; }
    public int Id { get; set; }

    public DateTime LoginDateTime { get; set; }

    public string Username { get; set; } = null!;

    public bool IsSuccessful { get; set; }

    public virtual User UserLoginNavigation { get; set; } = null!;
}
