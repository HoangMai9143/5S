using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using DC.Models;

namespace DC.Components.Dialog
{
  public partial class QuestionEditDialog
  {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

    [Parameter] public QuestionModel Question { get; set; } = new QuestionModel();

    private QuestionModel question = new QuestionModel();

    protected override void OnInitialized()
    {
      question = new QuestionModel
      {
        Id = Question.Id,
        QuestionContext = Question.QuestionContext,
        QuestionType = Question.QuestionType
      };
    }

    private void Submit()
    {
      MudDialog.Close(DialogResult.Ok(question));
    }

    private void Cancel() => MudDialog.Cancel();
  }
}