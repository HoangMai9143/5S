using System.Collections.Generic;
using System.Threading.Tasks;
using MudBlazor;
using DC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Web;
using DC.Components.Dialog;

namespace DC.Components.Pages
{
  public partial class Question
  {
    private List<QuestionModel> questions = new List<QuestionModel>();
    private string newQuestionText = string.Empty;
    private string _searchString = string.Empty;
    private bool _sortIdDescending = true;
    private List<string> _events = new();

    private Func<QuestionModel, object> _sortById => x =>
    {
      if (_sortIdDescending)
        return -x.Id;
      else
        return x.Id;
    };

    private Func<QuestionModel, bool> _quickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_searchString))
        return true;
      if (x.QuestionContext.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;
      if (x.Id.ToString().Contains(_searchString))
        return true;
      return false;
    };

    protected override async Task OnInitializedAsync()
    {
      try
      {
        questions = await appDbContext.Set<QuestionModel>().OrderByDescending(q => q.Id).ToListAsync();
        TotalQuestions = questions.Count;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading questions: {ex.Message}");
        questions = new List<QuestionModel>();
        TotalQuestions = 0;
      }
    }

    private async Task InsertQuestion()
    {
      if (!string.IsNullOrWhiteSpace(newQuestionText))
      {
        var newQuestion = new QuestionModel { QuestionContext = newQuestionText };
        await appDbContext.Set<QuestionModel>().AddAsync(newQuestion);
        await appDbContext.SaveChangesAsync();
        questions.Add(newQuestion);
        newQuestionText = string.Empty;
        StateHasChanged();
      }
    }

    private async Task DeleteQuestion(QuestionModel questionToDelete)
    {
      if (questionToDelete != null)
      {
        var parameters = new DialogParameters
{
{ "ContentText", "Are you sure you want to delete this question?" },
{ "ButtonText", "Delete" },
{ "Color", Color.Error }
};

        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall, Position = DialogPosition.Center };

        var dialog = await dialogService.ShowAsync<ConfirmDialog>("Delete Confirmation", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
          await ConfirmedDelete(questionToDelete);
        }
      }
    }

    private async Task ConfirmedDelete(QuestionModel questionToDelete)
    {
      appDbContext.Set<QuestionModel>().Remove(questionToDelete);
      await appDbContext.SaveChangesAsync();
      questions.Remove(questionToDelete);
      StateHasChanged();
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
      if (e.Key == "Enter")
      {
        await InsertQuestion();
      }
    }

    void RowClicked(DataGridRowClickEventArgs<QuestionModel> args)
    {
      _events.Insert(0, $"Event = RowClick, Index = {args.RowIndex}, Data = {System.Text.Json.JsonSerializer.Serialize(args.Item)}");
    }

    void RowRightClicked(DataGridRowClickEventArgs<QuestionModel> args)
    {
      _events.Insert(0, $"Event = RowRightClick, Index = {args.RowIndex}, Data = {System.Text.Json.JsonSerializer.Serialize(args.Item)}");
    }

    void SelectedItemsChanged(HashSet<QuestionModel> items)
    {
      _events.Insert(0, $"Event = SelectedItemsChanged, Data = {System.Text.Json.JsonSerializer.Serialize(items)}");
    }
  }
}