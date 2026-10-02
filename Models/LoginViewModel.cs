using System.ComponentModel.DataAnnotations;

namespace libraryMVC.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "User name or email is required.")]
        [Display(Name = "User Name or Email")]
        public string UserNameOrEmail { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; } = false;

        public string? ReturnUrl { get; set; } = string.Empty;
    }
}
