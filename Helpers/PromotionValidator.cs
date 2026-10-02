using Project.Models.Db;

namespace Project.Helpers
{
    /// <summary>
    /// ตรวจสอบสิทธิ์การใช้โปรโมชั่นก่อน Apply ที่ OrderController / CheckoutController
    /// </summary>
    public static class PromotionValidator
    {
        public enum ValidateResult
        {
            Valid,
            NotFound,
            Expired,
            NotStarted,
            NotActive,
            BelowMinOrder,
            UserTypeNotAllowed,
            UsageLimitReached,
            TotalLimitReached
        }

        public static ValidateResult Validate(
            Csi402dbbContext db,
            string promoCode,
            int userId,
            decimal orderTotal,
            out Promotion? promotion)
        {
            promotion = null;

            var promo = db.Promotions.FirstOrDefault(p => p.PromoCode == promoCode);
            if (promo == null) return ValidateResult.NotFound;
            if (promo.Status != 1) return ValidateResult.NotActive;

            var now = DateTime.Now;
            if (promo.StartDate.HasValue && promo.StartDate > now) return ValidateResult.NotStarted;
            if (promo.EndDate.HasValue   && promo.EndDate   < now) return ValidateResult.Expired;

            if (promo.MinOrderAmount.HasValue && orderTotal < promo.MinOrderAmount.Value)
                return ValidateResult.BelowMinOrder;

            if (promo.TotalUsageLimit.HasValue && promo.UsageCount >= promo.TotalUsageLimit.Value)
                return ValidateResult.TotalLimitReached;

            if (promo.AllowedUserType != 0)
            {
                var prevOrderCount = db.Orders.Count(o => o.UserId == userId);
                var user = db.Users.Find(userId);

                switch (promo.AllowedUserType)
                {
                    case 1: if (prevOrderCount > 0)           return ValidateResult.UserTypeNotAllowed; break;
                    case 2: if (prevOrderCount == 0)          return ValidateResult.UserTypeNotAllowed; break;
                    case 3: if (user == null || user.Role!=3) return ValidateResult.UserTypeNotAllowed; break;
                }
            }

            if (promo.UsageLimitPerUser.HasValue)
            {
                var timesUsed = db.Orders.Count(o => o.UserId == userId && o.PromotionId == promo.PromotionId);
                if (timesUsed >= promo.UsageLimitPerUser.Value)
                    return ValidateResult.UsageLimitReached;
            }

            promotion = promo;
            return ValidateResult.Valid;
        }

        public static decimal CalcDiscount(Promotion promo, decimal subtotal)
        {
            return promo.DiscountType == 1
                ? Math.Round(subtotal * promo.DiscountValue / 100, 2)
                : Math.Min(promo.DiscountValue, subtotal);
        }

        public static void IncrementUsage(Csi402dbbContext db, int promotionId)
        {
            var promo = db.Promotions.Find(promotionId);
            if (promo != null) promo.UsageCount++;
        }

        public static string GetErrorMessage(ValidateResult result, Promotion? promo = null)
        {
            return result switch
            {
                ValidateResult.NotFound           => "ไม่พบรหัสโปรโมชั่นนี้",
                ValidateResult.Expired            => "โปรโมชั่นนี้หมดอายุแล้ว",
                ValidateResult.NotStarted         => "โปรโมชั่นนี้ยังไม่เริ่มต้น",
                ValidateResult.NotActive          => "โปรโมชั่นนี้ยังไม่เปิดใช้งาน",
                ValidateResult.BelowMinOrder      => $"ยอดซื้อขั้นต่ำ ฿{promo?.MinOrderAmount:N0} เพื่อใช้โปรนี้",
                ValidateResult.TotalLimitReached  => "โปรโมชั่นนี้ถูกใช้หมดโควต้าแล้ว",
                ValidateResult.UsageLimitReached  => "คุณใช้โปรโมชั่นนี้ครบสิทธิ์แล้ว",
                ValidateResult.UserTypeNotAllowed => promo?.AllowedUserType switch {
                    1 => "โปรโมชั่นนี้สำหรับลูกค้าใหม่ที่ยังไม่เคยสั่งซื้อเท่านั้น",
                    2 => "โปรโมชั่นนี้สำหรับลูกค้าที่เคยสั่งซื้อแล้วเท่านั้น",
                    3 => "โปรโมชั่นนี้สำหรับสมาชิก VIP เท่านั้น",
                    _ => "คุณไม่มีสิทธิ์ใช้โปรโมชั่นนี้"
                },
                _ => "ไม่สามารถใช้โปรโมชั่นนี้ได้"
            };
        }
    }
}
