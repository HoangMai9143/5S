using System.Text.Json;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

using DC.Services;
using DC.ViewModels;

namespace DC.Components.Pages.Account
{
  public partial class Login
  {
    [Inject]
    private IJSRuntime js { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    [SupplyParameterFromForm]
    public LoginViewModel Model { get; set; } = new();
    private string? errorMessage;

    private async Task Authenticate()
    {
      try
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

        var userInfo = new UserInfo
        {
          Username = userAccount.Username,
          Role = userAccount.Role
        };

        await js.InvokeVoidAsync("localStorage.setItem", "userInfo", JsonSerializer.Serialize(userInfo));

        if (AuthStateProvider is CustomAuthenticationStateProvider customProvider)
        {
          await customProvider.UpdateAuthenticationState();
        }
        else
        {
          errorMessage = "Authentication provider is not available.";
          return;
        }

        navigationManager.NavigateTo("/home");
      }
      catch (Exception ex)
      {
        errorMessage = $"An error occurred: {ex.Message}";
        navigationManager.NavigateTo("/home");
      }
    }
  }
}