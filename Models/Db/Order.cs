using System;
using System.Collections.Generic;

namespace Project.Models.Db;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal TotalPrice { get; set; }

    public int? OrderStatus { get; set; }

    public int? PromotionId { get; set; }

    public DateTime? OrderDate { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Promotion? Promotion { get; set; }

    public virtual User User { get; set; } = null!;
}
