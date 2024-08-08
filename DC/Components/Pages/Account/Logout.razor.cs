using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DC.Components.Pages.Account
{
  public partial class Logout
  {
    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    protected override async Task OnInitializedAsync()
    {
      await base.OnInitializedAsync();
      await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "userInfo");
      navigationManager.NavigateTo("/home", true);
    }
  }
}