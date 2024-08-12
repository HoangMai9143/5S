using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DC.Components.Pages
{
  public partial class Home
  {
    private bool isLoading = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        isLoading = false;
        StateHasChanged();
      }
    }
  }
}