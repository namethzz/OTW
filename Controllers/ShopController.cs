using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Db;
using System.Text.Json;

namespace Project.Controllers
{
    public class ShopController : Controller
    {
        private readonly Csi402dbbContext _db;
        private const string CartKey = "Cart";

        public ShopController(Csi402dbbContext db)
        {
            _db = db;
        }
           //  SHOP — รายการสินค้า    
        public IActionResult Index(int? categoryId, string? search, string? sort)
        {
            var categories = _db.Categories.ToList();
            ViewBag.Categories   = categories;
            ViewBag.CategoryId   = categoryId;
            ViewBag.Search       = search;
            ViewBag.Sort         = sort;

            var query = _db.Products
                .Include(p => p.Category)
                .Where(p => p.Status == 1)
                .AsQueryable();

            // กรองหมวดหมู่
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // ค้นหา
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search));

            // เรียงลำดับ
            query = sort switch
            {
                "price_asc"  => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest"     => query.OrderByDescending(p => p.CreatedAt),
                _            => query.OrderBy(p => p.ProductId)
            };

            var products = query.ToList();

            // ดึง Reviews ทั้งหมดของสินค้าที่แสดง เพื่อคำนวณดาวในหน้า Index
            var productIds = products.Select(p => p.ProductId).ToList();
            var allReviews = _db.Reviews
                .Where(r => productIds.Contains(r.ProductId))
                .ToList();
            ViewBag.AllReviews = allReviews;

            return View(products);
        }

        //  PRODUCT DETAIL
        
        public IActionResult Detail(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id && p.Status == 1);

            if (product == null) return NotFound();

            // สินค้าในหมวดเดียวกัน
            var related = _db.Products
                .Where(p => p.CategoryId == product.CategoryId
                         && p.ProductId != id
                         && p.Status == 1)
                .Take(4).ToList();

            // ดึง review จาก DB
            var reviews = _db.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var reviewCount = reviews.Count;

            ViewBag.Related      = related;
            ViewBag.Reviews      = reviews;
            ViewBag.AvgRating    = avgRating;
            ViewBag.ReviewCount  = reviewCount;
            return View(product);
        }

        //  CART — ดูตะกร้า
        
        public IActionResult Cart()
        {
            var cart = GetCart();
            return View(cart);
        }

        //  CART — เพิ่มสินค้า
        
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = _db.Products.Find(productId);
            if (product == null) return NotFound();

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
                item.Quantity += quantity;
            else
                cart.Add(new CartItem
                {
                    ProductId   = product.ProductId,
                    ProductName = product.ProductName,
                    Price       = product.Price,
                    ImageUrl    = product.ImageUrl ?? "",
                    Quantity    = quantity
                });

            SaveCart(cart);

            TempData["CartSuccess"] = $"เพิ่ม {product.ProductName} ลงตะกร้าแล้ว!";
            return RedirectToAction("Index");
        }

        //  CART — อัปเดตจำนวน
        
        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0) cart.Remove(item);
                else item.Quantity = quantity;
            }

            SaveCart(cart);
            return RedirectToAction("Cart");
        }

        //  CART — ลบสินค้า
       
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProductId == productId);
            SaveCart(cart);
            return RedirectToAction("Cart");
        }

        
        //  CART — นับจำนวน (สำหรับ Navbar)
        
        public IActionResult GetCartCount()
        {
            var count = GetCart().Sum(c => c.Quantity);
            return Json(count);
        }

       
        
        
        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartKey);
            return json == null
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json)!;
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }
    }

    
    //  CartItem Model (ไม่ต้องมี DB table)
    
    public class CartItem
    {
        public int     ProductId   { get; set; }
        public string  ProductName { get; set; } = "";
        public decimal Price       { get; set; }
        public string  ImageUrl    { get; set; } = "";
        public int     Quantity    { get; set; }
        public decimal Subtotal    => Price * Quantity;
    }
}
