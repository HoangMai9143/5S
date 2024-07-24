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
    private Timer debounceTimer;

    private QuestionType selectedQuestionType = QuestionType.MultipleChoice; // Default value

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
      await LoadQuestionsAsync();
    }

    private async Task LoadQuestionsAsync()
    {
      try
      {
        questions = await appDbContext.Set<QuestionModel>()
            .OrderByDescending(q => q.Id)
            .ToListAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading questions: {ex.Message}");
        sb.Add("Error loading questions", Severity.Error);
        questions = new List<QuestionModel>();
      }
    }

    private async Task InsertQuestion()
    {
      if (!string.IsNullOrWhiteSpace(newQuestionText))
      {
        var existingQuestion = await appDbContext.Set<QuestionModel>()
            .FirstOrDefaultAsync(q => q.QuestionContext.ToLower() == newQuestionText.ToLower());

        if (existingQuestion != null)
        {
          sb.Add("This question already exists.", Severity.Warning);
          _searchString = newQuestionText; // Set the search string to the new question text
          await SearchQuestions(_searchString); // Filter the questions to show the existing one
          StateHasChanged(); // Update the UI
          return;
        }

        var newQuestion = new QuestionModel
        {
          QuestionContext = newQuestionText,
          QuestionType = selectedQuestionType
        };
        await appDbContext.Set<QuestionModel>().AddAsync(newQuestion);
        await appDbContext.SaveChangesAsync();

        await LoadQuestionsAsync(); // Refresh the questions list

        newQuestionText = string.Empty;
        sb.Add($"Question added successfully with ID: {newQuestion.Id}", Severity.Success);
        StateHasChanged();
      }
    }

    private async Task DeleteQuestion(QuestionModel questionToDelete)
    {
      if (questionToDelete != null)
      {
        int questionToDeleteId = questionToDelete.Id;
        var parameters = new DialogParameters
        {
            { "ContentText", "Are you sure you want to delete this question?" },
            { "ButtonText", "Delete" },
            { "Color", Color.Error }
        };

        var options = new DialogOptions()
        {
          MaxWidth = MaxWidth.Small,
          FullWidth = true,
          Position = DialogPosition.Center,
          CloseOnEscapeKey = true,
          FullScreen = false,
        };

        var dialog = await dialogService.ShowAsync<ConfirmDialog>("Delete Question", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
          await ConfirmedDelete(questionToDelete);
          sb.Add($"Question {questionToDeleteId} deleted", Severity.Success);
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

    private async Task OpenEditDialog(QuestionModel questionToEdit)
    {
      var parameters = new DialogParameters
    {
        { "Question", questionToEdit }
    };

      var options = new DialogOptions
      {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        Position = DialogPosition.Center,
        CloseOnEscapeKey = true,
        FullScreen = false,
      };

      var dialog = await dialogService.ShowAsync<QuestionEditDialog>("Edit Question", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled && result.Data is QuestionModel updatedQuestion)
      {
        await UpdateQuestion(updatedQuestion); // Update the question with the edited details
        StateHasChanged();
      }
    }

    private async Task UpdateQuestion(QuestionModel questionToUpdate)
    {
      var trackedQuestion = appDbContext.ChangeTracker.Entries<QuestionModel>()
          .FirstOrDefault(e => e.Entity.Id == questionToUpdate.Id)?.Entity;

      if (trackedQuestion != null)
      {
        appDbContext.Entry(trackedQuestion).State = EntityState.Detached;
      }

      appDbContext.Set<QuestionModel>().Update(questionToUpdate);
      await appDbContext.SaveChangesAsync();
      sb.Add($"Question {questionToUpdate.Id} updated", Severity.Success);

      // Refresh the questions list by calling LoadQuestionsAsync
      await LoadQuestionsAsync();
      StateHasChanged();
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
      if (e.Key == "Enter")
      {
        await InsertQuestion();
      }
      StateHasChanged();
    }
    private async Task OnSearchInput(string value)
    {
      _searchString = value; // Update the search term
    }
    private async Task SearchQuestions(string searchTerm)
    {
      if (string.IsNullOrWhiteSpace(searchTerm))
      {
        await LoadQuestionsAsync(); // Load all questions if search term is empty
      }
      else
      {
        questions = await appDbContext.Set<QuestionModel>()
            .Where(q => q.QuestionContext.Contains(searchTerm))
            .OrderByDescending(q => q.Id)
            .ToListAsync();
      }
    }
  }
}