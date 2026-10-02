using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Db;
using Project.Models;

namespace Project.Controllers;

public class HomeController : Controller
{
    private readonly Csi402dbbContext _db;

    public HomeController(Csi402dbbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        var products = _db.Products
            .Include(p => p.Category)
            .Where(p => p.Status == 1)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToList();

        var categories = _db.Categories.ToList();

        var reviews = _db.Reviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => !string.IsNullOrEmpty(r.Comment))
            .OrderByDescending(r => r.CreatedAt)
            .Take(6)
            .ToList();

        ViewBag.Products   = products;
        ViewBag.Categories = categories;
        ViewBag.Reviews    = reviews;
        ViewBag.MinPrice   = 1500;

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId != null)
        {
            var orders = _db.Orders
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            ViewBag.Orders = orders;
        }

        return View();
    }

    // ── หน้าโปรโมชั่น (Customer) ──────────────────────────────
    public IActionResult Promo()
    {
        var now = DateTime.Now;
        var promos = _db.Promotions
            .Where(p => p.Status == 1 && p.EndDate > now)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
        return View(promos);
    }

    // ── Profile ───────────────────────────────────────────────
    [HttpGet]
    public IActionResult Profile()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = _db.Users.Find(userId.Value);
        if (user == null) return RedirectToAction("Login", "Account");

        if (TempData["ProfileSuccess"] != null)
            ViewBag.ProfileSuccess = TempData["ProfileSuccess"];
        if (TempData["ProfileError"] != null)
            ViewBag.ProfileError = TempData["ProfileError"];

        return View(user);
    }

    [HttpPost]
    public IActionResult UpdateProfile(string fullName, string phone, string? currentPassword, string? newPassword, IFormFile? avatarFile)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = _db.Users.Find(userId.Value);
        if (user == null) return RedirectToAction("Login", "Account");

        // อัปเดตชื่อ
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            user.FullName = fullName.Trim();
            HttpContext.Session.SetString("FullName", user.FullName);
        }

        // อัปเดตเบอร์โทร
        user.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        // เปลี่ยนรหัสผ่าน
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || user.Password != currentPassword)
            {
                TempData["ProfileError"] = "รหัสผ่านปัจจุบันไม่ถูกต้อง";
                return RedirectToAction("Profile");
            }
            if (newPassword.Length < 6)
            {
                TempData["ProfileError"] = "รหัสผ่านใหม่ต้องมีอย่างน้อย 6 ตัวอักษร";
                return RedirectToAction("Profile");
            }
            user.Password = newPassword;
        }

        // อัปโหลดรูปโปรไฟล์
        if (avatarFile != null && avatarFile.Length > 0)
        {
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(avatarFile.FileName).ToLower();
            if (!allowedExt.Contains(ext))
            {
                TempData["ProfileError"] = "รองรับเฉพาะไฟล์ JPG, PNG, WEBP เท่านั้น";
                return RedirectToAction("Profile");
            }
            if (avatarFile.Length > 3 * 1024 * 1024)
            {
                TempData["ProfileError"] = "ขนาดไฟล์ต้องไม่เกิน 3 MB";
                return RedirectToAction("Profile");
            }

            var uploadDir = Path.Combine("wwwroot", "images", "avatars");
            Directory.CreateDirectory(uploadDir);
            var fileName = $"avatar_{userId.Value}{ext}";
            var savePath = Path.Combine(uploadDir, fileName);
            using var stream = System.IO.File.Create(savePath);
            avatarFile.CopyTo(stream);
            user.ImageUrl = $"/images/avatars/{fileName}";
        }

        _db.SaveChanges();
        TempData["ProfileSuccess"] = "อัปเดตโปรไฟล์เรียบร้อยแล้ว";
        return RedirectToAction("Profile");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
