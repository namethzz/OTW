using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Db;
using Project.ViewModels;

namespace Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly Csi402dbbContext _context;

        public AccountController(Csi402dbbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn() =>
            HttpContext.Session.GetInt32("UserId") != null;

        private int GetUserId() =>
            HttpContext.Session.GetInt32("UserId")!.Value;

        // ==================== INDEX (ประวัติออเดอร์) ====================

        public IActionResult Index()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();

            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // ดึงคำขอคืน/เปลี่ยนสินค้าทั้งหมดของ user
            var myReturns = _context.Returns
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            ViewBag.Orders = orders;
            ViewBag.MyReturns = myReturns;

            return View("Orders");
        }

        // ==================== REGISTER ====================

        [HttpGet]
        public IActionResult Register()
        {
            if (IsLoggedIn())
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool emailExists = _context.Users.Any(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "อีเมลนี้ถูกใช้งานแล้ว");
                return View(model);
            }

            var user = new User
            {
                Username = model.Email,
                Password = model.Password,
                FullName = model.Name,
                Email = model.Email,
                Role = 2,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetInt32("Role", user.Role);

            return RedirectToAction("Index", "Home");
        }

        // ==================== LOGIN ====================

        [HttpGet]
        public IActionResult Login()
        {
            if (IsLoggedIn())
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users
                .FirstOrDefault(u => u.Email == model.Email
                                  && u.Password == model.Password);

            if (user == null)
            {
                TempData["LoginError"] = "อีเมลหรือรหัสผ่านไม่ถูกต้อง";
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetInt32("Role", user.Role);

            if (user.Role == 1)
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Index", "Home");
        }

        // ==================== LOGOUT ====================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }


        // ==================== ORDER DETAIL ====================

        public IActionResult OrderDetail(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();

            var order = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .Include(o => o.Promotion)
                .FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
                return RedirectToAction("Index");

            var returnRequest = _context.Returns
                .FirstOrDefault(r => r.OrderId == id && r.UserId == userId);

            // รีวิวที่ user เขียนไปแล้วสำหรับ order นี้
            var myReviews = _context.Reviews
                .Where(r => r.OrderId == id && r.UserId == userId)
                .ToList();

            ViewBag.ReturnRequest = returnRequest;
            ViewBag.MyReviews     = myReviews;

            return View("Detail", order);
        }
        // ==================== MY RETURNS (ประวัติขอคืน/เปลี่ยน) ====================

        public IActionResult MyReturns()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();

            var myReturns = _context.Returns
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            // ดึง Order ที่เกี่ยวข้องเพื่อแสดงข้อมูลประกอบ
            var orderIds = myReturns.Select(r => r.OrderId).Distinct().ToList();
            var relatedOrders = _context.Orders
                .Where(o => orderIds.Contains(o.OrderId))
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .ToList();

            ViewBag.MyReturns = myReturns;
            ViewBag.RelatedOrders = relatedOrders;

            return View("MyReturns");
        }

        // ==================== USER LIST (Admin only) ====================

        public IActionResult UserList()
        {
            var users = _context.Users.ToList();
            return View(users);
        }
    }
}