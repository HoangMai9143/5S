using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DC.Components.Pages
{
  public partial class Grading
  {
    private bool isLoading = true;
    private int activeIndex = 0;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        isLoading = false;
        StateHasChanged();
      }
    }
    private async Task HandleTabChanged(int index)
    {

      activeIndex = index;
    }
  }
}