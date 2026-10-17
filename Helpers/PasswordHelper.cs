using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Project.Models.Db;

namespace Project.Helpers
{
    /// <summary>
    /// Hash / ตรวจรหัสผ่านด้วย PasswordHasher ของ ASP.NET Core
    /// รองรับรหัสผ่านเก่าที่เก็บเป็น plain text: ตรวจได้ และแจ้งให้ hash ใหม่ (needsRehash)
    /// </summary>
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<User> Hasher = new();

        public static string Hash(User user, string password) =>
            Hasher.HashPassword(user, password);

        public static bool Verify(User user, string password, out bool needsRehash)
        {
            needsRehash = false;

            if (IsHashed(user.Password))
            {
                var result = Hasher.VerifyHashedPassword(user, user.Password, password);
                needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
                return result != PasswordVerificationResult.Failed;
            }

            // รหัสผ่านเดิมที่ยังเป็น plain text
            var ok = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.Password),
                Encoding.UTF8.GetBytes(password));
            needsRehash = ok;
            return ok;
        }

        // Hash รูปแบบ V3 ของ PasswordHasher: base64 ที่ขึ้นต้นด้วย byte 0x01 และยาวอย่างน้อย 61 bytes
        private static bool IsHashed(string stored)
        {
            try
            {
                var bytes = Convert.FromBase64String(stored);
                return bytes.Length >= 61 && bytes[0] == 0x01;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
