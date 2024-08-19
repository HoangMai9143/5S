using Microsoft.AspNetCore.Components;

using DC.Models;

using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class ChooseQuestionsDialog
  {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

    [Parameter] public SurveyModel Survey { get; set; }
    [Parameter] public List<QuestionModel> Questions { get; set; }
    [Parameter] public HashSet<QuestionModel> SelectedQuestions { get; set; }
    [Parameter] public HashSet<int> ExistingQuestionIds { get; set; }

    private string searchString = string.Empty;

    private Func<QuestionModel, bool> quickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(searchString))
        return true;

      if (x.Id.ToString().Contains(searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.QuestionContext.Contains(searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    };

    void Cancel() => MudDialog.Cancel();

    void Submit() => MudDialog.Close(DialogResult.Ok(SelectedQuestions));
  }
}