using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DC.Components.Pages
{
  public partial class Counter
  {
    int currentCount { get; set; }

    protected override void OnInitialized()
    {
      base.OnInitialized();
      currentCount = 0;
    }

    protected void IncrementCount()
    {
      currentCount++;
    }

  }
}