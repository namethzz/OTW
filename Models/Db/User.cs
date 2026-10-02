using System;
using System.Collections.Generic;

namespace Project.Models.Db;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int Role { get; set; }

    public DateTime? CreatedAt { get; set; }

    // รูปโปรไฟล์ (optional)
    public string? ImageUrl { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
