using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class AutoGradeDialog
  {
    [CascadingParameter] MudDialogInstance mudDialog { get; set; }
    [Parameter] public int bias { get; set; }
    [Parameter] public int deviation { get; set; }


    void Submit() => mudDialog.Close(DialogResult.Ok(true));
    void Cancel() => mudDialog.Cancel();
  }
}