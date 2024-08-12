using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DC.Components.Pages.Account
{
  public partial class Logout
  {
    [Inject]
    private IJSRuntime js { get; set; }

    protected override async Task OnInitializedAsync()
    {
      await base.OnInitializedAsync();
      await js.InvokeVoidAsync("localStorage.removeItem", "userInfo");
      navigationManager.NavigateTo("/home", true);
    }
  }
}