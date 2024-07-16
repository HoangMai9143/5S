using System.ComponentModel.DataAnnotations;

namespace DC.ViewModels
{
  public class LoginViewModel
  {
    [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide User Name")]
    public string? userName { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide Password")]
    public string? password { get; set; }
  }
}