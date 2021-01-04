using System.ComponentModel.DataAnnotations;

namespace helpDesk.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }
    }

    public class ManageUserViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Одоогийн нууц үг")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "{0} дор хаяж {2} тэмдэгтээс бүрдсэн байх ёстой.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Шинэ нууц үг")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Шинэ нууц үг дахин оруулах")]
        [Compare("NewPassword", ErrorMessage = "Баталгаажуулж оруулсан нууц үг таарахгүй байна.")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Нэвтрэх нэр заавал оруулна уу.")]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required(ErrorMessage="Нууц үг заавал оруулна уу.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [Display(Name="Байгууллага")]
        public int organisation { get; set; }
        [Required]
        [Display(Name = "Нэвтрэх нэр")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Нууц үгийн урт хамгийн багадаа 6 байна.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Нууц үг")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Нууц үг давтах")]
        [Compare("Password", ErrorMessage = "Давтан оруулсан нууц үг тохирохгүй байна.")]
        public string ConfirmPassword { get; set; }
    }
}
