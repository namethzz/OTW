namespace Project.Helpers
{
    /// <summary>
    /// ที่เก็บไฟล์ที่ผู้ใช้อัปโหลด (รูปสินค้า รูปโปรไฟล์ หลักฐานคืนสินค้า)
    /// ตั้ง path ได้ด้วย config "Uploads:Path" (env: Uploads__Path) เพื่อชี้ไปที่ volume
    /// ที่ไม่หายตอน deploy ใหม่; ถ้าไม่ตั้งจะใช้ wwwroot/uploads
    /// ไฟล์ทั้งหมดเสิร์ฟที่ URL /uploads/...
    /// </summary>
    public class UploadStorage
    {
        public const string RequestPath = "/uploads";

        public string RootPath { get; }

        public UploadStorage(string rootPath)
        {
            RootPath = Path.GetFullPath(rootPath);
            Directory.CreateDirectory(RootPath);
        }

        // บันทึกไฟล์ลง {RootPath}/{folder}/{fileName} แล้วคืน URL สำหรับเก็บใน DB
        public string Save(IFormFile file, string folder, string fileName)
        {
            var dir = Path.Combine(RootPath, folder);
            Directory.CreateDirectory(dir);

            using var stream = File.Create(Path.Combine(dir, fileName));
            file.CopyTo(stream);

            return $"{RequestPath}/{folder}/{fileName}";
        }
    }
}
