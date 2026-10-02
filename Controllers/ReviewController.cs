using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Db;

namespace Project.Controllers
{
    public class ReviewController : Controller
    {
        private readonly Csi402dbbContext _db;

        public ReviewController(Csi402dbbContext db)
        {
            _db = db;
        }

        private bool IsLoggedIn() =>
            HttpContext.Session.GetInt32("UserId") != null;

        private int GetUserId() =>
            HttpContext.Session.GetInt32("UserId")!.Value;

        [HttpGet]
        public IActionResult Create(int orderId, int productId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();

            var order = _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
                return RedirectToAction("Index", "Account");

            if (order.OrderStatus != 3)
            {
                TempData["ReviewError"] = "สามารถรีวิวได้เฉพาะออเดอร์ที่จัดส่งสำเร็จแล้วเท่านั้น";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            var orderItem = order.OrderDetails.FirstOrDefault(od => od.ProductId == productId);
            if (orderItem == null)
            {
                TempData["ReviewError"] = "ไม่พบสินค้านี้ในออเดอร์";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            var existingReview = _db.Reviews.FirstOrDefault(r =>
                r.OrderId == orderId &&
                r.ProductId == productId &&
                r.UserId == userId);

            if (existingReview != null)
            {
                TempData["ReviewError"] = "คุณรีวิวสินค้านี้ไปแล้ว";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            ViewBag.Order = order;
            ViewBag.Product = orderItem.Product;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int orderId, int productId, int rating, string? comment)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();

            var order = _db.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
                return RedirectToAction("Index", "Account");

            if (order.OrderStatus != 3)
            {
                TempData["ReviewError"] = "สามารถรีวิวได้เฉพาะออเดอร์ที่จัดส่งสำเร็จแล้วเท่านั้น";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            var orderItem = order.OrderDetails.FirstOrDefault(od => od.ProductId == productId);
            if (orderItem == null)
            {
                TempData["ReviewError"] = "ไม่พบสินค้านี้ในออเดอร์";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "กรุณาให้คะแนน 1 ถึง 5 ดาว";
                return RedirectToAction("Create", new { orderId, productId });
            }

            var existingReview = _db.Reviews.FirstOrDefault(r =>
                r.OrderId == orderId &&
                r.ProductId == productId &&
                r.UserId == userId);

            if (existingReview != null)
            {
                TempData["ReviewError"] = "คุณรีวิวสินค้านี้ไปแล้ว";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            var review = new Review
            {
                OrderId = orderId,
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _db.Reviews.Add(review);
            _db.SaveChanges();

            TempData["ReviewSuccess"] = "ส่งรีวิวเรียบร้อยแล้ว";
            return RedirectToAction("OrderDetail", "Account", new { id = orderId });
        }
    }
}