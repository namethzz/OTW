# OTW — Minimal Premium UI

ปรับหน้าร้านและ Admin โดยดัดแปลง CommerceHero กับ ProductRevealCard ที่แนบมาให้ทำงานกับ ASP.NET Core MVC เดิม

## สิ่งที่ปรับ

- หน้าร้าน: Commerce Hero, ลิงก์หมวดหมู่จากฐานข้อมูล และการ์ดสินค้าใหม่ทั้งหน้า Home และ Shop
- Admin: พื้นหลังขาวอุ่น สีดำอมเขียว ฟอนต์ไทย ระยะห่าง เมนูด้านข้าง topbar ตาราง ฟอร์ม และ Dashboard
- Admin Products: สลับตาราง/การ์ดได้ ค้นหาและกรองสถานะ/สต็อกร่วมกัน แสดงข้อความเมื่อไม่มีผลลัพธ์
- Product Reveal Card: เปิดรายละเอียดด้วยปุ่ม disclosure ที่รองรับเมาส์ คีย์บอร์ด และการแตะ; ยังคงเห็นราคาและปุ่มดำเนินการชัดเจน
- ใช้รูป ราคา สต็อก และรีวิวจริง ไม่ใส่คะแนน ส่วนลด หรือฟังก์ชัน favorites จำลอง
- เมนูมือถือ: Escape เพื่อปิด คืน focus ล็อกการเลื่อน และป้องกัน focus หลุดไปเนื้อหาด้านหลัง
- รองรับ prefers-reduced-motion
- คะแนนสินค้าหน้า Home คำนวณจากรีวิวทั้งหมดของสินค้านั้น แยกจากรีวิวแนะนำ 6 รายการ

## ไฟล์ที่แก้ / เพิ่ม

| ไฟล์ | หน้าที่ |
|---|---|
| Views/Shared/Components/UI/_CommerceHero.cshtml | Hero และทางลัด / หมวดหมู่ |
| Views/Shared/Components/UI/_ProductRevealCard.cshtml | การ์ดสินค้า ใช้ซ้ำทั้งลูกค้าและ Admin |
| ViewModels/CommerceHeroViewModel.cs | ข้อมูล Hero |
| ViewModels/ProductRevealCardViewModel.cs | ข้อมูลการ์ดและบริบท Admin |
| wwwroot/css/commerce-components.css | รูปแบบ components และ responsive |
| wwwroot/css/admin-premium.css | ธีม Admin ทั้งระบบ |
| wwwroot/js/admin-premium.js | เมนู Admin บน desktop / mobile |
| Views/Shared/_AdminLayout.cshtml | โครง Admin และโหลด assets |
| Views/Shared/_Layout.cshtml | โหลดรูปแบบ components หน้าร้าน |
| Views/Admin/Dashboard.cshtml | Hero และทางลัดจัดการร้าน |
| Views/Admin/Products.cshtml | การ์ด ตาราง ค้นหา ตัวกรอง |
| Views/Home/Index.cshtml | หน้าร้านและสินค้าแนะนำ |
| Views/Shop/Index.cshtml | การ์ดสินค้าหน้า Shop |
| Controllers/HomeController.cs | ดึงรีวิวของสินค้าแนะนำให้ครบ |

ถ้าต้องการนำไปใส่โปรเจกต์เดิม ให้สำรองงานแล้วคัดลอกเฉพาะไฟล์ในตารางไปไว้ตาม path เดิม ไม่ต้องแทนที่ appsettings.json หรือฐานข้อมูล

## วิธีรันบน Windows

ต้องมี .NET 10 SDK และ MySQL ที่ใช้กับโปรเจกต์เดิม ตรวจว่า connection string ของเครื่องคุณถูกต้อง

เปิด Command Prompt ในโฟลเดอร์ที่มี Project.csproj เช่น:

```bat
cd /d D:\402\Project
dotnet restore
dotnet build
dotnet run --launch-profile http
```

เปิด http://localhost:5071 แล้วเข้าสู่ระบบด้วยบัญชี Admin เดิมเพื่อดู /Admin/Dashboard

โปรเจกต์นี้เป็น C# / Razor ไม่ใช่ Node.js จึงไม่ใช้ npm run start

## ทำไมไม่ได้วางไฟล์ React .tsx ตรง ๆ

ตัวอย่างที่แนบมาใช้ React + shadcn + Tailwind + TypeScript แต่เว็บนี้เรนเดอร์ด้วย Razor และใช้ Bootstrap อยู่แล้ว การวาง .tsx อย่างเดียวจะไม่ถูกคอมไพล์หรือแสดงผล จึงแปลงเป็น Razor partial components และ CSS/JavaScript มาตรฐาน โดยใช้ Views/Shared/Components/UI เป็นที่รวม UI ที่หลายหน้าเรียกซ้ำได้

ไม่ต้องติดตั้ง React, shadcn, Tailwind, TypeScript หรือ framer-motion เพื่อรันงานชุดนี้ ความเคลื่อนไหวใช้ CSS และเคารพ reduced-motion ส่วนข้อมูลยังใช้ Models/Controllers และฟอร์มเดิม

## การตรวจสอบและข้อจำกัด

- ผ่าน JavaScript syntax check
- ผ่านการทดสอบ logic ด้วย DOM จำลอง: ค้นหา, กรองสถานะร่วมกับสต็อก, สลับตาราง/การ์ด, ไม่พบผลลัพธ์, ย่อเมนู, เปิดเมนูมือถือ, inert/focus, Escape และเปลี่ยนขนาดหน้าจอ
- ยังไม่ได้ build Razor/C# หรือรันกับฐานข้อมูลจริง เนื่องจากสภาพแวดล้อมแก้ไขไม่มี .NET SDK และ MySQL ของผู้ใช้
- ยังไม่ได้ตรวจ screenshot / layout ในเบราว์เซอร์จริง เพราะติดตั้ง Chromium ไม่สำเร็จ
- ฟอนต์ Google, Bootstrap CDN และ Tabler CDN ยังต้องมีอินเทอร์เน็ตตามการใช้ CDN ของโปรเจกต์เดิม

หลังรันบนเครื่อง ให้ตรวจ Home, Shop, Admin Dashboard, Products (ตาราง/การ์ด), เพิ่ม/แก้สินค้า, Orders, Returns, Promotions และ Users ที่ desktop และมือถือ พร้อมลองตะกร้าและฟอร์มจริงก่อนนำไปใช้งาน

ชุดนี้เป็นการปรับ UI ไม่ใช่การตรวจความปลอดภัยหรือแก้ระบบธุรกิจทั้งหมด
