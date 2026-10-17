using Microsoft.AspNetCore.Mvc;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Project.Helpers;
using Project.Models.Db; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// ตรวจ antiforgery token ทุก POST/PUT/DELETE เพื่อกัน CSRF
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddDbContext<Csi402dbbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    )
);

// ที่เก็บไฟล์อัปโหลด: ตั้ง Uploads__Path ให้ชี้ไป volume บน server เพื่อไม่ให้ไฟล์หายตอน deploy
var uploadStorage = new UploadStorage(
    builder.Configuration["Uploads:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));
builder.Services.AddSingleton(uploadStorage);

// ✅ เพิ่ม Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ไฟล์อัปโหลดถูกสร้างตอนรันไทม์ จึงไม่อยู่ใน manifest ของ MapStaticAssets ต้องเสิร์ฟแยก
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadStorage.RootPath),
    RequestPath = UploadStorage.RequestPath
});
// รูปที่อัปโหลดไว้ก่อนหน้านี้ใน wwwroot/images/products และ wwwroot/images/avatars
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ✅ เพิ่ม UseSession (ต้องอยู่ก่อน MapControllerRoute)
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
