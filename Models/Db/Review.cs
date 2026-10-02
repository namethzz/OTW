using System;

namespace Project.Models.Db
{
    public partial class Review
    {
        public int ReviewId { get; set; }

        public int ProductId { get; set; }

        public int UserId { get; set; }

        public int OrderId { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual Product Product { get; set; } = null!;

        public virtual User User { get; set; } = null!;

        public virtual Order Order { get; set; } = null!;
    }
}