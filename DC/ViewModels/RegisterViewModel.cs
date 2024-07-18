using System.ComponentModel.DataAnnotations;

namespace DC.ViewModels
{
  public class RegisterViewModel
  {
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm Password is required")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
  }
}