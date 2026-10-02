using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Db;

namespace Project.Controllers
{
    public class ReturnController : Controller
    {
        private readonly Csi402dbbContext _db;
        private readonly IWebHostEnvironment _env;

        public ReturnController(Csi402dbbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private bool IsLoggedIn() =>
            HttpContext.Session.GetInt32("UserId") != null;

        private int GetUserId() =>
            HttpContext.Session.GetInt32("UserId")!.Value;

        [HttpGet]
        public IActionResult Create(int orderId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();

            var order = _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
                return RedirectToAction("Index", "Home");

            // ยื่นคืน/เปลี่ยนได้เฉพาะออเดอร์ที่จัดส่งสำเร็จแล้ว
            if (order.OrderStatus != 3)
            {
                TempData["ReturnError"] = "สามารถขอคืนเงินหรือเปลี่ยนสินค้าได้เฉพาะออเดอร์ที่จัดส่งสำเร็จแล้วเท่านั้น";
                return RedirectToAction("Index", "Home");
            }

            var existingReturn = _db.Returns
                .FirstOrDefault(r => r.OrderId == orderId && r.Status == 0);

            if (existingReturn != null)
            {
                TempData["ReturnError"] = "ออเดอร์นี้มีคำขอคืนที่กำลังรอตรวจสอบอยู่แล้ว";
                return RedirectToAction("Index", "Home");
            }

            var deliveredDate = order.OrderDate ?? DateTime.Now;
            ViewBag.Order = order;
            ViewBag.DeadlineDate = deliveredDate.AddDays(7).ToString("d MMM yyyy");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            int orderId,
            string returnType,
            string reason,
            string? note,
            IFormFile? image)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();

            var order = _db.Orders
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
                return RedirectToAction("Index", "Home");

            if (order.OrderStatus != 3)
            {
                TempData["ReturnError"] = "สามารถขอคืนเงินหรือเปลี่ยนสินค้าได้เฉพาะออเดอร์ที่จัดส่งสำเร็จแล้วเท่านั้น";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(returnType) || string.IsNullOrWhiteSpace(reason))
            {
                TempData["ReturnError"] = "กรุณากรอกข้อมูลให้ครบถ้วน";
                return RedirectToAction("Create", new { orderId });
            }

            var existingReturn = _db.Returns
                .FirstOrDefault(r => r.OrderId == orderId && r.Status == 0);

            if (existingReturn != null)
            {
                TempData["ReturnError"] = "ออเดอร์นี้มีคำขอคืนที่กำลังรอตรวจสอบอยู่แล้ว";
                return RedirectToAction("Index", "Home");
            }

            string? imageUrl = null;

            if (image != null && image.Length > 0)
            {
                var allowedExt = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(image.FileName).ToLower();

                if (!allowedExt.Contains(ext))
                {
                    TempData["ReturnError"] = "รองรับเฉพาะไฟล์ JPG และ PNG เท่านั้น";
                    return RedirectToAction("Create", new { orderId });
                }

                if (image.Length > 5 * 1024 * 1024)
                {
                    TempData["ReturnError"] = "ขนาดไฟล์ต้องไม่เกิน 5 MB";
                    return RedirectToAction("Create", new { orderId });
                }

                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "returns");
                Directory.CreateDirectory(uploadDir);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadDir, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);

                imageUrl = $"/uploads/returns/{fileName}";
            }

            _db.Returns.Add(new Return
            {
                OrderId = orderId,
                UserId = userId,
                ReturnType = returnType,
                Reason = reason,
                Note = note,
                ImageUrl = imageUrl,
                Status = 0,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            TempData["ReturnSuccess"] = "ยื่นคำขอคืนสินค้าเรียบร้อยแล้ว";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult History()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();

            var returns = _db.Returns
                .Where(r => r.UserId == userId)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(returns);
        }
    }
}