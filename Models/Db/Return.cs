using System;
using System.Collections.Generic;

namespace Project.Models.Db;

public partial class Return
{
    public int ReturnId { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    // "refund" | "exchange"
    public string ReturnType { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public string? Note { get; set; }

    public string? ImageUrl { get; set; }

    // 0 = รอตรวจสอบ, 1 = อนุมัติ, 2 = ปฏิเสธ
    public int Status { get; set; }

    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? User { get; set; }
}
