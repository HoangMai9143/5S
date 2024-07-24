using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class QuestionEditDialog
  {
    [CascadingParameter] MudDialogInstance mudDialog { get; set; }

    [Parameter] public string questionText { get; set; }

    void Submit()
    {
      mudDialog.Close(DialogResult.Ok(questionText));
    }
    void Cancel() => mudDialog.Cancel();
  }
}