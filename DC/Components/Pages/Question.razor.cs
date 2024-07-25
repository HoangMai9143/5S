using System.Collections.Generic;
using System.Threading.Tasks;
using MudBlazor;
using DC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Web;
using DC.Components.Dialog;
using System.Timers;

namespace DC.Components.Pages
{
  public partial class Question
  {
    private int activeIndex = 0;
    private List<QuestionModel> questions = new List<QuestionModel>();
    private List<AnswerModel> answers = new();
    private HashSet<AnswerModel> selectedAnswers = new();
    private HashSet<int> existingAnswerIds = new();
    private HashSet<int> originalExistingAnswerIds = new();
    private QuestionModel selectedQuestion;

    private string _searchString = string.Empty;
    private bool _sortIdDescending = true;
    private System.Timers.Timer _debounceTimer;
    private const int DebounceDelay = 300; // milliseconds

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
      {
        return true;
      }
      return false;
    };

    protected override async Task OnInitializedAsync()
    {
      await LoadQuestions();
      _debounceTimer = new System.Timers.Timer(DebounceDelay);
      _debounceTimer.Elapsed += async (sender, e) => await DebounceTimerElapsed();
      _debounceTimer.AutoReset = false;
    }

    private async Task LoadQuestions()
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
        };
        await appDbContext.Set<QuestionModel>().AddAsync(newQuestion);
        await appDbContext.SaveChangesAsync();

        await LoadQuestions();

        _searchString = string.Empty;
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
      await LoadQuestions();
      StateHasChanged();
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
      if (e.Key == "Enter")
      {
        _debounceTimer.Stop();
        await DebounceTimerElapsed();
      }
    }

    private void OnSearchInput(string value)
    {
      _searchString = value;
      _debounceTimer.Stop();
      _debounceTimer.Start();
    }

    private async Task DebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        if (string.IsNullOrWhiteSpace(_searchString))
        {
          await InsertQuestion();
        }
        else
        {
          await SearchQuestions(_searchString);
        }
        StateHasChanged();
      });
    }

    private async Task SearchQuestions(string searchTerm)
    {
      if (string.IsNullOrWhiteSpace(searchTerm))
      {
        await LoadQuestions(); // Load all questions if search term is empty
      }
      else
      {
        searchTerm = searchTerm.ToLower(); // Convert search term to lowercase
        questions = await appDbContext.Set<QuestionModel>()
            .Where(q => q.QuestionContext.ToLower().Contains(searchTerm))
            .OrderByDescending(q => q.Id)
            .ToListAsync();
      }
    }

    private async Task HandleTabChanged(int index)
    {
      // if (index == 1 && selectedQuestion == null)
      // {
      //   sb.Add("Please select a question first.", Severity.Warning);

      //   return;
      // }
      if (index == 0 && selectedQuestion != null)
      {
        selectedQuestion = null;
        selectedAnswers.Clear();
      }
      if (selectedQuestion != null)
      {
        await LoadExistingAnswer();
      }
      activeIndex = index;
    }

    private async Task LoadExistingAnswer()
    {
      var existingAnswerIdsList = await appDbContext.Set<QuestionAnswerModel>()
          .Where(sq => sq.QuestionId == selectedQuestion.Id)
          .Select(sq => sq.AnswerId)
          .ToListAsync();

      // Convert to hashset for faster lookup
      existingAnswerIds = new HashSet<int>(existingAnswerIds);
      originalExistingAnswerIds = new HashSet<int>(originalExistingAnswerIds);
      selectedAnswers = new HashSet<AnswerModel>(answers.Where(q => existingAnswerIds.Contains(q.Id)));
      StateHasChanged();
    }

    private async Task CloneQuestion(QuestionModel questionToClone)
    {
      // Clone the question
      var clonedQuestion = new QuestionModel
      {
        QuestionContext = questionToClone.QuestionContext
      };

      await appDbContext.Set<QuestionModel>().AddAsync(clonedQuestion);
      await appDbContext.SaveChangesAsync();

      // Clone the answers associated with the question
      var questionAnswers = await appDbContext.Set<QuestionAnswerModel>()
          .Where(qa => qa.QuestionId == questionToClone.Id)
          .ToListAsync();

      foreach (var qa in questionAnswers)
      {
        var clonedAnswer = new AnswerModel
        {
          QuestionId = clonedQuestion.Id,
          AnswerText = qa.Answer.AnswerText,
          Points = qa.Answer.Points,
          AnswerType = qa.Answer.AnswerType
        };

        await appDbContext.Set<AnswerModel>().AddAsync(clonedAnswer);
        await appDbContext.SaveChangesAsync();

        await appDbContext.Set<QuestionAnswerModel>().AddAsync(new QuestionAnswerModel
        {
          QuestionId = clonedQuestion.Id,
          AnswerId = clonedAnswer.Id
        });
      }

      await appDbContext.SaveChangesAsync();
      await LoadQuestions(); // Assuming you have a method to reload questions
      sb.Add($"Question {questionToClone.Id} cloned successfully with ID: {clonedQuestion.Id}", Severity.Success);
    }
  }
}