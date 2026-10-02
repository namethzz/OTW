using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Db;

namespace Project.Controllers
{
    public class AdminController : Controller
    {
        private readonly Csi402dbbContext _db;

        public AdminController(Csi402dbbContext db)
        {
            _db = db;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetInt32("Role") == 1;
        }

        // ══════════════════════════════════════════
        //  DASHBOARD
        // ══════════════════════════════════════════
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.TotalProducts = _db.Products.Count();
            ViewBag.TotalOrders = _db.Orders.Count();
            ViewBag.TotalUsers = _db.Users.Count(u => u.Role == 2);
            ViewBag.TotalRevenue = _db.Orders.Sum(o => (decimal?)o.TotalPrice) ?? 0;
            ViewBag.LowStock = _db.Products.Count(p => p.Stock < 10);
            ViewBag.PendingOrders = _db.Orders.Count(o => o.OrderStatus == 1);
            ViewBag.PendingReturns = _db.Returns.Count(r => r.Status == 0);

            var recentOrders = _db.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();
            ViewBag.RecentOrders = recentOrders;

            var lowStockProducts = _db.Products
                .Where(p => p.Stock < 10)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToList();
            ViewBag.LowStockProducts = lowStockProducts;

            return View();
        }

        // ══════════════════════════════════════════
        //  PRODUCTS
        // ══════════════════════════════════════════
        public IActionResult Products()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var products = _db.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Categories = _db.Categories.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult AddProduct(Product model, IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var savePath = Path.Combine("wwwroot/images/products", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                using var stream = System.IO.File.Create(savePath);
                imageFile.CopyTo(stream);
                model.ImageUrl = "/images/products/" + fileName;
            }

            model.CreatedAt = DateTime.Now;
            model.Status = 1;
            _db.Products.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "เพิ่มสินค้าเรียบร้อยแล้ว";
            return RedirectToAction("Products");
        }

        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = _db.Products.Find(id);
            if (product == null) return NotFound();
            ViewBag.Categories = _db.Categories.ToList();
            return View(product);
        }

        [HttpPost]
        public IActionResult EditProduct(Product model, IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = _db.Products.Find(model.ProductId);
            if (product == null) return NotFound();

            product.ProductName = model.ProductName;
            product.CategoryId = model.CategoryId;
            product.Price = model.Price;
            product.Stock = model.Stock;
            product.Description = model.Description;
            product.Status = model.Status;

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var savePath = Path.Combine("wwwroot/images/products", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                using var stream = System.IO.File.Create(savePath);
                imageFile.CopyTo(stream);
                product.ImageUrl = "/images/products/" + fileName;
            }

            _db.SaveChanges();
            TempData["Success"] = "แก้ไขสินค้าเรียบร้อยแล้ว";
            return RedirectToAction("Products");
        }

        public IActionResult DeleteProduct(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = _db.Products.Find(id);
            if (product != null)
            {
                // ตรวจว่ามี OrderDetail อ้างอิงสินค้านี้อยู่หรือไม่
                bool hasOrders = _db.OrderDetails.Any(od => od.ProductId == id);
                if (hasOrders)
                {
                    // ปิดการขายแทนการลบ เพื่อรักษาประวัติออเดอร์
                    product.Status = 0;
                    _db.SaveChanges();
                    TempData["Success"] = "สินค้านี้มีประวัติการสั่งซื้อ จึงปิดการขายแทนการลบ";
                }
                else
                {
                    // ลบ Review ของสินค้านี้ก่อน (ถ้ามี)
                    var reviews = _db.Reviews.Where(r => r.ProductId == id).ToList();
                    _db.Reviews.RemoveRange(reviews);

                    _db.Products.Remove(product);
                    _db.SaveChanges();
                    TempData["Success"] = "ลบสินค้าเรียบร้อยแล้ว";
                }
            }
            return RedirectToAction("Products");
        }

        // ══════════════════════════════════════════
        //  ORDERS
        // ══════════════════════════════════════════
        public IActionResult Orders()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var orders = _db.Orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }

        public IActionResult OrderDetail(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(int orderId, int status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = _db.Orders.Find(orderId);
            if (order != null)
            {
                order.OrderStatus = status;
                _db.SaveChanges();
            }
            return RedirectToAction("Orders");
        }

        // ══════════════════════════════════════════
        //  RETURNS
        // ══════════════════════════════════════════
        public IActionResult Returns()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var returns = _db.Returns
                .Include(r => r.User)
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            return View(returns);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReviewReturn(int returnId, int status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var ret = _db.Returns
                .Include(r => r.Order)
                .FirstOrDefault(r => r.ReturnId == returnId);

            if (ret != null)
            {
                ret.Status = status;
                ret.ReviewedAt = DateTime.Now;
                _db.SaveChanges();

                TempData["ReturnMsg"] = status == 1
                    ? $"อนุมัติคำขอ #{returnId} เรียบร้อยแล้ว"
                    : $"ปฏิเสธคำขอ #{returnId} เรียบร้อยแล้ว";
            }

            return RedirectToAction("Returns");
        }

        // ══════════════════════════════════════════
        //  PROMOTIONS
        // ══════════════════════════════════════════
        public IActionResult Promotions()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var promos = _db.Promotions
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
            return View(promos);
        }

        [HttpGet]
        public IActionResult AddPromotion()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult AddPromotion(Promotion model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            model.CreatedAt = DateTime.Now;
            model.Status = 1;
            model.UsageCount = 0; // เริ่มต้นที่ 0 ครั้ง
            _db.Promotions.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "เพิ่มโปรโมชั่นเรียบร้อยแล้ว";
            return RedirectToAction("Promotions");
        }

        public IActionResult DeletePromotion(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var promo = _db.Promotions.Find(id);
            if (promo != null)
            {
                // ตรวจว่ามี Order ที่ใช้โปรนี้อยู่หรือไม่
                bool hasOrders = _db.Orders.Any(o => o.PromotionId == id);
                if (hasOrders)
                {
                    // ปิดการใช้งานแทนการลบ เพื่อรักษาประวัติออเดอร์
                    promo.Status = 0;
                    // ทำให้หมดอายุทันที
                    promo.EndDate = DateTime.Now.AddSeconds(-1);
                    _db.SaveChanges();
                    TempData["Success"] = "โปรโมชั่นนี้มีประวัติการใช้งาน จึงปิดใช้งานแทนการลบ";
                }
                else
                {
                    _db.Promotions.Remove(promo);
                    _db.SaveChanges();
                    TempData["Success"] = "ลบโปรโมชั่นเรียบร้อยแล้ว";
                }
            }
            return RedirectToAction("Promotions");
        }

        // ══════════════════════════════════════════
        //  USERS
        // ══════════════════════════════════════════
        public IActionResult Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var users = _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            // นับจำนวน Order ของแต่ละ User เพื่อแยกลูกค้าใหม่/เก่า
            var orderCounts = _db.Orders
                .GroupBy(o => o.UserId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.OrderCounts = orderCounts;
            return View(users);
        }

        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _db.Users.Find(id);
            if (user != null && user.Role != 1)
            {
                // ลบ Reviews ของ user นี้ก่อน (FK: fk_reviews_user)
                var reviews = _db.Reviews.Where(r => r.UserId == id).ToList();
                _db.Reviews.RemoveRange(reviews);

                // ลบ Returns ของ user นี้ก่อน (FK: returns_ibfk_2)
                var returns = _db.Returns.Where(r => r.UserId == id).ToList();
                _db.Returns.RemoveRange(returns);

                // ลบ OrderDetails ของทุก Order ของ user นี้ก่อน
                var orderIds = _db.Orders.Where(o => o.UserId == id).Select(o => o.OrderId).ToList();
                var orderDetails = _db.OrderDetails.Where(od => orderIds.Contains(od.OrderId)).ToList();
                _db.OrderDetails.RemoveRange(orderDetails);

                // ลบ Orders ของ user นี้
                var orders = _db.Orders.Where(o => o.UserId == id).ToList();
                _db.Orders.RemoveRange(orders);

                // ลบ User
                _db.Users.Remove(user);
                _db.SaveChanges();

                TempData["Success"] = "ลบสมาชิกเรียบร้อยแล้ว";
            }
            return RedirectToAction("Users");
        }
    }
}
