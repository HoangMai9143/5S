using System.Security.Claims;
using DC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DC.Components.Pages.Account
{
  public partial class Login
  {
    [CascadingParameter]
    public HttpContext? httpContext { get; set; }

    [SupplyParameterFromForm]
    public LoginViewModel Model { get; set; } = new();
    private string? errorMessage;

    private async Task Authenticate()
    {
      if (appDbContext == null)
      {
        errorMessage = "Database context is not available.";
        return;
      }
      var userAccount = await appDbContext.UserAccountModel.FirstOrDefaultAsync(x => x.Username == Model.userName && x.Password == Model.password);
      if (userAccount == null)
      {
        errorMessage = "Invalid User Name or Password";
        return;
      }
      var claims = new List<Claim>
      {
          new Claim(ClaimTypes.Name, userAccount.Username),
          new Claim(ClaimTypes.Role, userAccount.Role)
      };
      var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
      var principal = new ClaimsPrincipal(identity);
      await httpContext.SignInAsync(principal);
      navigationManager.NavigateTo("/home");
    }
  }
}