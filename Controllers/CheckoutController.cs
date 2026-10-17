using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Db;
using Project.Helpers;
using System.Text.Json;

namespace Project.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly Csi402dbbContext _db;
        private const string CartKey = "Cart";

        public CheckoutController(Csi402dbbContext db)
        {
            _db = db;
        }

        private bool IsLoggedIn() =>
            HttpContext.Session.GetInt32("UserId") != null;

        [HttpGet]
        public IActionResult Index(string? code)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");
            // ส่ง code ไปยัง View เพื่อ pre-fill (ถ้ามาจากหน้าโปรโมชั่น)
            if (!string.IsNullOrEmpty(code))
                ViewBag.PrefilledCode = code.ToUpper();

            var cart = GetCart();
            if (!cart.Any())
                return RedirectToAction("Cart", "Shop");

            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = _db.Users.Find(userId);
            var subtotal = cart.Sum(c => c.Subtotal);
            var shipping = subtotal >= 1500 ? 0m : 50m;

            ViewBag.Cart = cart;
            ViewBag.User = user;
            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.Total = subtotal + shipping;

            // ── กรองโปรโมชั่นที่อยู่ในช่วงเวลา + ตรวจสิทธิ์ลูกค้า ──
            var now = DateTime.Now;
            var allPromos = _db.Promotions
                .Where(p => p.Status == 1 && p.StartDate <= now && p.EndDate >= now)
                .ToList();

            // กรองเฉพาะโปรที่ลูกค้าคนนี้มีสิทธิ์ใช้
            var eligiblePromos = allPromos.Where(p =>
            {
                var r = PromotionValidator.Validate(_db, p.PromoCode!, userId, subtotal, out _);
                return r == PromotionValidator.ValidateResult.Valid;
            }).ToList();

            ViewBag.Promotions = eligiblePromos;

            // แสดง error จากการ PlaceOrder ครั้งก่อน (ถ้ามี)
            if (TempData["PromoError"] != null)
                ViewBag.PromoError = TempData["PromoError"];

            return View();
        }

        [HttpPost]
        public IActionResult PlaceOrder(
            string fullName, string phone,
            string houseNo, string road,
            string subDistrict, string district,
            string province, string zipCode,
            int? promotionId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (!cart.Any())
                return RedirectToAction("Cart", "Shop");

            var userId = HttpContext.Session.GetInt32("UserId")!.Value;

            // ── ใช้ราคาปัจจุบันจาก DB และตรวจว่าสินค้ายังเปิดขาย / สต็อกพอ ──
            var productIds = cart.Select(c => c.ProductId).ToList();
            var products = _db.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionary(p => p.ProductId);

            foreach (var item in cart)
            {
                if (!products.TryGetValue(item.ProductId, out var product) || product.Status != 1)
                {
                    TempData["CartError"] = $"{item.ProductName} ไม่มีจำหน่ายแล้ว กรุณาลบออกจากตะกร้า";
                    return RedirectToAction("Cart", "Shop");
                }
                if (item.Quantity < 1 || item.Quantity > product.Stock)
                {
                    TempData["CartError"] = $"{item.ProductName} เหลือในสต็อกเพียง {product.Stock} ชิ้น";
                    return RedirectToAction("Cart", "Shop");
                }
                item.Price = product.Price;
            }

            var subtotal = cart.Sum(c => c.Subtotal);
            var shipping = subtotal >= 1500 ? 0m : 50m;

            // ── ตรวจสอบโปรโมชั่นด้วย PromotionValidator ──
            // ตรวจซ้ำอีกครั้ง ณ เวลาที่กดสั่งซื้อจริง (อาจหมดอายุหลังจากเลือกไว้แล้ว)
            decimal discount = 0;
            Promotion? usedPromo = null;
            int? appliedPromotionId = null;

            if (promotionId.HasValue && promotionId.Value > 0)
            {
                var promo = _db.Promotions.Find(promotionId.Value);
                if (promo != null)
                {
                    // ตรวจสอบ ณ เวลาปัจจุบันทันที (ป้องกันเลือกตอน 23:59 แต่กดสั่ง 00:01)
                    var now = DateTime.Now;
                    bool isExpiredNow = promo.EndDate.HasValue && promo.EndDate.Value < now;
                    bool isNotStarted = promo.StartDate.HasValue && promo.StartDate.Value > now;

                    if (isExpiredNow || isNotStarted)
                    {
                        TempData["PromoError"] = isExpiredNow
                            ? $"โปรโมชั่น \"{promo.PromoName}\" หมดอายุแล้ว กรุณาตรวจสอบและสั่งซื้อใหม่อีกครั้ง"
                            : "โปรโมชั่นนี้ยังไม่เริ่มต้น";
                        return RedirectToAction("Index");
                    }

                    var result = PromotionValidator.Validate(
                        _db, promo.PromoCode!, userId, subtotal, out var validPromo);

                    if (result == PromotionValidator.ValidateResult.Valid && validPromo != null)
                    {
                        discount = PromotionValidator.CalcDiscount(validPromo, subtotal);
                        usedPromo = validPromo;
                        appliedPromotionId = validPromo.PromotionId;
                    }
                    else
                    {
                        TempData["PromoError"] = PromotionValidator.GetErrorMessage(result, promo);
                        return RedirectToAction("Index");
                    }
                }
            }

            var totalPrice = subtotal - discount + shipping;
            var fullAddress = $"{houseNo} {road} ต.{subDistrict} อ.{district} จ.{province} {zipCode}".Trim();

            // ── สร้าง Order ──
            var order = new Order
            {
                UserId = userId,
                Subtotal = subtotal,
                DiscountAmount = discount,
                TotalPrice = totalPrice,
                PromotionId = appliedPromotionId,
                OrderStatus = 1,
                OrderDate = DateTime.Now
            };

            // ทำทั้งหมดใน transaction เดียว ถ้าสต็อกหรือโควต้าโปรไม่พอระหว่างทางจะ rollback ทั้งออเดอร์
            using var tx = _db.Database.BeginTransaction();

            _db.Orders.Add(order);
            _db.SaveChanges(); // ต้อง Save ก่อนเพื่อได้ OrderId

            // ── บันทึก OrderDetail และลด Stock ──
            foreach (var item in cart)
            {
                _db.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });

                // ลดสต็อกแบบมีเงื่อนไขใน SQL เดียว กันขายเกินเมื่อมีคนสั่งพร้อมกัน
                var updated = _db.Products
                    .Where(p => p.ProductId == item.ProductId && p.Stock >= item.Quantity)
                    .ExecuteUpdate(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));

                if (updated == 0)
                {
                    tx.Rollback();
                    TempData["CartError"] = $"{item.ProductName} สต็อกไม่พอแล้ว กรุณาปรับจำนวนในตะกร้า";
                    return RedirectToAction("Cart", "Shop");
                }
            }

            // ── เพิ่ม UsageCount ของโปรโมชั่น ──
            if (usedPromo != null && !PromotionValidator.TryIncrementUsage(_db, usedPromo.PromotionId))
            {
                tx.Rollback();
                TempData["PromoError"] = PromotionValidator.GetErrorMessage(
                    PromotionValidator.ValidateResult.TotalLimitReached, usedPromo);
                return RedirectToAction("Index");
            }

            _db.SaveChanges();
            tx.Commit();

            TempData["OrderAddress"] = fullAddress;
            TempData["OrderPhone"] = phone;
            TempData["OrderFullName"] = fullName;

            HttpContext.Session.Remove(CartKey);
            return RedirectToAction("Success", new { orderId = order.OrderId });
        }

        public IActionResult Success(int orderId)
        {
            var order = _db.Orders.Find(orderId);
            if (order == null || order.UserId != HttpContext.Session.GetInt32("UserId"))
                return RedirectToAction("Index", "Home");

            var orderDetails = _db.OrderDetails
                .Where(od => od.OrderId == orderId)
                .Include(od => od.Product)
                .ToList();

            ViewBag.Order = order;
            ViewBag.OrderDetails = orderDetails;
            ViewBag.FullName = TempData["OrderFullName"] ?? HttpContext.Session.GetString("FullName");
            ViewBag.Address = TempData["OrderAddress"];
            ViewBag.Phone = TempData["OrderPhone"];

            return View();
        }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartKey);
            return json == null
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json)!;
        }
    }
}