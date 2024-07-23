using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class AlertDialog
  {

    [CascadingParameter] MudDialogInstance mudDialog { get; set; }
    [Parameter] public string message { get; set; }

    private void Close()
    {
      mudDialog.Close(DialogResult.Ok(true));
    }

  }
}