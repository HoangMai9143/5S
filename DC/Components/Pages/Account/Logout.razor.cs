using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication;

namespace DC.Components.Pages.Account
{
  public partial class Logout
  {
    [CascadingParameter]
    public HttpContext? httpContext { get; set; }

    protected override async Task OnInitializedAsync()
    {
      await base.OnInitializedAsync();
      if (httpContext.User.Identity.IsAuthenticated)
      {
        await httpContext.SignOutAsync();
        navigationManager.NavigateTo("/logout", true);
      }
    }
  }
}