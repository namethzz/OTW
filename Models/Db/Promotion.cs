using System;
using System.Collections.Generic;

namespace Project.Models.Db;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string? PromoCode { get; set; }

    public string PromoName { get; set; } = null!;

    public int DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    // ─── field ควบคุมสิทธิ์ ───────────────────────────
    // 0 = ทุกคน, 1 = ลูกค้าใหม่, 2 = ลูกค้าเก่า, 3 = VIP
    public int AllowedUserType { get; set; } = 0;

    // จำกัดจำนวนครั้งที่ใช้ต่อคน (null = ไม่จำกัด)
    public int? UsageLimitPerUser { get; set; }

    // จำกัดโควต้ารวมทั้งโปรโมชั่น (null = ไม่จำกัด)
    public int? TotalUsageLimit { get; set; }

    // นับจำนวนครั้งที่ถูกใช้ไปแล้วทั้งหมด
    public int UsageCount { get; set; } = 0;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}