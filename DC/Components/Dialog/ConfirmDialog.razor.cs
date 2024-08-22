using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class ConfirmDialog
  {

    [CascadingParameter] MudDialogInstance mudDialog { get; set; }

    [Parameter] public string contentText { get; set; }
    [Parameter] public string buttonText { get; set; }
    [Parameter] public Color color { get; set; }

    void Submit() => mudDialog.Close(DialogResult.Ok(true));
    void Cancel() => mudDialog.Cancel();
  }
}