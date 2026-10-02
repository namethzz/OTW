using System.ComponentModel.DataAnnotations;

namespace Project.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        public string Email { get; set; }

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
