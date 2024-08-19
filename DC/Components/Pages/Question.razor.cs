using Microsoft.EntityFrameworkCore;

using DC.Components.Dialog;
using DC.Models;

using MudBlazor;

namespace DC.Components.Pages
{
  public partial class Question
  {
    private const int DEBOUNCE_DELAY = 300; // ms

    private bool isLoading = true; // Loading bar

    private List<QuestionModel> questionsList = new(); // List of questions to display
    private string _searchString = string.Empty; // Search bar string to filter questions
    private AnswerType _selectedAnswerType = AnswerType.SingleChoice; // Default answer type
    private System.Timers.Timer? _questionDebounceTimer; // Timer for search debounce

    private Func<QuestionModel, bool> _questionQuickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_searchString))
        return true;

      if (x.Id.ToString().Contains(_searchString))
        return true;

      if (x.QuestionContext.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        await LoadQuestions();
        _questionDebounceTimer = new System.Timers.Timer(DEBOUNCE_DELAY);
        _questionDebounceTimer.Elapsed += async (sender, e) => await QuestionDebounceTimerElapsed();
        _questionDebounceTimer.AutoReset = false;

        isLoading = false;
        StateHasChanged();
      }
    }

    private async Task LoadQuestions()
    {
      try
      {
        questionsList = await appDbContext.Set<QuestionModel>()
            .Include(q => q.Answers)
            .OrderByDescending(q => q.Id)
            .ToListAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading questions: {ex.Message}");
        sb.Add("Error loading , reloading page...", Severity.Error);
        questionsList = new List<QuestionModel>();
        await Task.Delay(1000);
        await LoadQuestions();
      }
    }

    //* Use search string to check if question already exists in database else insert new question with selected answer type (or default to SingleChoice). 
    private async Task InsertQuestion()
    {
      if (!string.IsNullOrWhiteSpace(_searchString))
      {
        var existingQuestion = await appDbContext.Set<QuestionModel>()
            .FirstOrDefaultAsync(q => q.QuestionContext.ToLower() == _searchString.ToLower());

        if (existingQuestion != null)
        {
          sb.Add("This question already exists.", Severity.Warning);
          await SearchQuestions(_searchString);
          StateHasChanged();
          return;
        }

        var newQuestion = new QuestionModel
        {
          QuestionContext = _searchString,
          AnswerType = _selectedAnswerType
        };
        await appDbContext.Set<QuestionModel>().AddAsync(newQuestion);
        await appDbContext.SaveChangesAsync();

        await LoadQuestions();

        _searchString = string.Empty;
        sb.Add($"Question added successfully with ID: {newQuestion.Id}", Severity.Success);
        await OpenQuestionEditDialog(newQuestion.Id);
        StateHasChanged();
      }
    }

    //* Search for questions based on search term (ID or question context)
    private async Task SearchQuestions(string _searchTerm)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
          await LoadQuestions();
        }
        else
        {
          _searchTerm = _searchTerm.ToLower();
          questionsList = await appDbContext.Set<QuestionModel>()
              .Where(q => q.Id.ToString().Contains(_searchTerm) ||
                          q.QuestionContext.ToLower().Contains(_searchTerm))
              .OrderByDescending(q => q.Id)
              .ToListAsync();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error searching questions: {ex.Message}");
        sb.Add("An error occurred while searching questions. Please try again later.", Severity.Error);
      }
    }

    //* Open dialog to confirm deletion of question (ConfirmDialog)
    private async Task OpenQuestionDeleteDialog(QuestionModel questionToDelete)
    {
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
        await DeleteQuestion(questionToDelete);
      }
    }

    //* Delete question and reload question list if that question is not being used (because of foreign key constraint)
    private async Task DeleteQuestion(QuestionModel questionToDelete)
    {
      try
      {
        appDbContext.Set<QuestionModel>().Remove(questionToDelete);
        await appDbContext.SaveChangesAsync();
        await LoadQuestions();
        sb.Add($"Question {questionToDelete.Id} deleted successfully.", Severity.Success);
      }
      catch (Exception ex)
      {
        sb.Add("Can't delete this question, it's currently being used", Severity.Error);
      }
    }

    //* Clone question with question context, answer type and answers
    private async Task CloneQuestion(QuestionModel _questionToClone)
    {
      try
      {
        var clonedQuestion = new QuestionModel
        {
          QuestionContext = _questionToClone.QuestionContext,
          AnswerType = _questionToClone.AnswerType
        };

        await appDbContext.Set<QuestionModel>().AddAsync(clonedQuestion);
        await appDbContext.SaveChangesAsync();

        foreach (var answer in _questionToClone.Answers)
        {
          var clonedAnswer = new AnswerModel
          {
            AnswerText = answer.AnswerText,
            Points = answer.Points,
            QuestionId = clonedQuestion.Id
          };
          await appDbContext.Set<AnswerModel>().AddAsync(clonedAnswer);
        }

        await appDbContext.SaveChangesAsync();
        await LoadQuestions();
        sb.Add($"Question {_questionToClone.Id} cloned successfully with ID: {clonedQuestion.Id}", Severity.Success);
      }
      catch (Exception ex)
      {
        sb.Add($"Error cloning question: {ex.Message}", Severity.Error);
      }
    }

    //* Update datagrid according on search input with debounce (to prevent multiple queries)
    private async Task OnQuestionSearchInput(string value)
    {
      _searchString = value;
      _questionDebounceTimer?.Stop();
      _questionDebounceTimer?.Start();
    }

    //* Debounce timer elapsed
    private async Task QuestionDebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchQuestions(_searchString);
        StateHasChanged();
      });
    }

    //* Open dialog to edit question (QuestionEditDialog)
    private async Task OpenQuestionEditDialog(int questionId)
    {
      try
      {
        var parameters = new DialogParameters
      {
        { "QuestionId", questionId }
      };
        var options = new DialogOptions { CloseOnEscapeKey = true, FullScreen = true, CloseButton = true };
        var dialog = await dialogService.ShowAsync<QuestionEditDialog>("Edit Question", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
          await LoadQuestions();
          StateHasChanged();
        }
      }
      catch (Exception ex)
      {
        sb.Add("An error occurred while opening the dialog. Please try again later.", Severity.Error);
      }
    }
  }
}