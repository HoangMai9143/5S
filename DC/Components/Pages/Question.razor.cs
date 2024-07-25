using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Components.Dialog;
using DC.Models;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Microsoft.AspNetCore.Components.Web;
using System.Timers;

namespace DC.Components.Pages
{
  public partial class Question
  {
    private int activeIndex = 0;
    private List<QuestionModel> questions = new List<QuestionModel>();
    private List<AnswerModel> answers = new List<AnswerModel>();
    private HashSet<AnswerModel> selectedAnswers = new HashSet<AnswerModel>();
    private HashSet<int> existingAnswerIds = new HashSet<int>();
    private HashSet<int> originalExistingAnswerIds = new HashSet<int>();
    private QuestionModel selectedQuestion;
    private System.Timers.Timer _debounceTimer;
    private string _searchString = string.Empty;
    private string _answerSearchString = string.Empty;

    private System.Timers.Timer _questionDebounceTimer;
    private System.Timers.Timer _answerDebounceTimer;
    private const int DebounceDelay = 300; // milliseconds

    // Filter questions
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

    // Filter answers
    private Func<AnswerModel, bool> _answerQuickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_answerSearchString))
        return true;

      if (x.Id.ToString().Contains(_answerSearchString))
        return true;

      if (x.AnswerText.Contains(_answerSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.Points.ToString().Contains(_answerSearchString))
        return true;

      if (x.AnswerType.ToString().Contains(_answerSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    };

    protected override async Task OnInitializedAsync()
    {
      await LoadQuestions();
      await LoadAnswers();

      _questionDebounceTimer = new System.Timers.Timer(DebounceDelay);
      _questionDebounceTimer.Elapsed += async (sender, e) => await QuestionDebounceTimerElapsed();
      _questionDebounceTimer.AutoReset = false;

      _answerDebounceTimer = new System.Timers.Timer(DebounceDelay);
      _answerDebounceTimer.Elapsed += async (sender, e) => await AnswerDebounceTimerElapsed();
      _answerDebounceTimer.AutoReset = false;
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
    private async Task LoadAnswers()
    {
      try
      {
        answers = await appDbContext.Set<AnswerModel>().ToListAsync();
        if (selectedQuestion != null)
        {
          await LoadExistingAnswers();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading answers: {ex.Message}");
        sb.Add("Error loading answers", Severity.Error);
        answers = new List<AnswerModel>();
      }
    }

    private async Task LoadExistingAnswers()
    {
      var existingAnswerIdsList = await appDbContext.Set<QuestionAnswerModel>()
          .Where(qa => qa.QuestionId == selectedQuestion.Id)
          .Select(qa => qa.AnswerId.Value)
          .ToListAsync();

      existingAnswerIds = new HashSet<int>(existingAnswerIdsList);
      originalExistingAnswerIds = new HashSet<int>(existingAnswerIds);
      selectedAnswers = new HashSet<AnswerModel>(answers.Where(a => existingAnswerIds.Contains(a.Id)));
      StateHasChanged();
    }

    private async Task HandleTabChanged(int index)
    {
      if (index == 1 && selectedQuestion == null)
      {
        sb.Add("Please select a question first.", Severity.Warning);
        return;
      }
      if (index == 0 && selectedQuestion != null)
      {
        selectedQuestion = null;
        selectedAnswers.Clear();
      }
      if (selectedQuestion != null)
      {
        await LoadExistingAnswers();
      }
      activeIndex = index;
    }

    private async Task OpenCreateQuestionDialog()
    {
      var dialog = dialogService.Show<QuestionCreateDialog>("Create New Question");
      var result = await dialog.Result;

      if (!result.Canceled && result.Data is QuestionModel newQuestion)
      {
        await LoadQuestions();
        sb.Add($"Question created successfully with ID: {newQuestion.Id}.", Severity.Success);
      }
    }

    private async Task SaveQuestionAnswers()
    {
      if (selectedQuestion == null || !selectedAnswers.Any())
      {
        sb.Add("Please select a question and at least one answer.", Severity.Warning);
        return;
      }

      try
      {
        // Get current and existing answer IDs
        var currentAnswerIds = selectedAnswers.Select(a => a.Id).ToHashSet();

        // Determine answers to remove and add
        var answersToRemove = existingAnswerIds.Except(currentAnswerIds).ToList();
        var answersToAdd = currentAnswerIds.Except(existingAnswerIds).ToList();

        // Update database
        foreach (var answerId in answersToRemove)
        {
          var questionAnswer = await appDbContext.Set<QuestionAnswerModel>()
              .FirstOrDefaultAsync(qa => qa.QuestionId == selectedQuestion.Id && qa.AnswerId == answerId);
          if (questionAnswer != null)
            appDbContext.Set<QuestionAnswerModel>().Remove(questionAnswer);
        }

        await appDbContext.Set<QuestionAnswerModel>().AddRangeAsync(
            answersToAdd.Select(answerId => new QuestionAnswerModel { QuestionId = selectedQuestion.Id, AnswerId = answerId })
        );

        await appDbContext.SaveChangesAsync();

        // Update existingAnswerIds to reflect the changes
        existingAnswerIds = new HashSet<int>(currentAnswerIds);
        originalExistingAnswerIds = new HashSet<int>(existingAnswerIds);

        sb.Add("Question answers saved to database successfully.", Severity.Success);
      }
      catch (Exception ex)
      {
        sb.Add($"Error saving question answers: {ex.Message}", Severity.Error);
      }
      finally
      {
        StateHasChanged();
      }
    }

    private void OnSelectionChanged(HashSet<AnswerModel> selectedItems)
    {
      selectedAnswers = selectedItems;
      StateHasChanged();
    }

    private void OnSelectQuestion(QuestionModel question)
    {
      selectedQuestion = question;
      LoadExistingAnswers();
      activeIndex = 1;
    }

    private async Task OpenEditQuestionDialog(QuestionModel questionToEdit)
    {
      var parameters = new DialogParameters
      {
        { "Question", questionToEdit }
      };

      var options = new DialogOptions()
      {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        Position = DialogPosition.Center,
        CloseOnEscapeKey = true,
        FullScreen = false,
      };

      var dialog = await dialogService.ShowAsync<QuestionEditDialog>("Edit Question", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled)
      {
        await LoadQuestions();
        sb.Add($"Question {questionToEdit.Id} updated successfully.", Severity.Success);
      }
    }

    private async Task DeleteQuestion(QuestionModel questionToDelete)
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
        await ConfirmedDeleteQuestion(questionToDelete);
        sb.Add($"Question {questionToDelete.Id} deleted successfully.", Severity.Success);
      }
    }

    private async Task ConfirmedDeleteQuestion(QuestionModel questionToDelete)
    {
      appDbContext.Set<QuestionModel>().Remove(questionToDelete);
      await appDbContext.SaveChangesAsync();
      await LoadQuestions();
    }

    private async Task CloneQuestion(QuestionModel questionToClone)
    {
      var clonedQuestion = new QuestionModel
      {
        QuestionContext = questionToClone.QuestionContext
      };

      await appDbContext.Set<QuestionModel>().AddAsync(clonedQuestion);
      await appDbContext.SaveChangesAsync();

      var questionAnswers = await appDbContext.Set<QuestionAnswerModel>()
          .Where(qa => qa.QuestionId == questionToClone.Id)
          .ToListAsync();

      foreach (var answer in questionAnswers)
      {
        await appDbContext.Set<QuestionAnswerModel>().AddAsync(new QuestionAnswerModel
        {
          QuestionId = clonedQuestion.Id,
          AnswerId = answer.AnswerId
        });
      }

      await appDbContext.SaveChangesAsync();
      await LoadQuestions();
      sb.Add($"Question {questionToClone.Id} cloned successfully with ID: {clonedQuestion.Id}", Severity.Success);
    }
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
      if (e.Key == "Enter")
      {
        _questionDebounceTimer.Stop();
        await DebounceTimerElapsed();
      }
    }
    private async Task OnQuestionSearchInput(string value)
    {
      _searchString = value;
      _questionDebounceTimer.Stop();
      _questionDebounceTimer.Start();
    }

    private async Task QuestionDebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchQuestions(_searchString);
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
            .Where(q => q.Id.ToString().Contains(searchTerm) ||
                        q.QuestionContext.ToLower().Contains(searchTerm))
            .OrderByDescending(q => q.Id)
            .ToListAsync();
      }
    }

    private async Task OnAnswerSearchInput(string value)
    {
      _answerSearchString = value;
      _answerDebounceTimer.Stop();
      _answerDebounceTimer.Start();
    }

    private async Task AnswerDebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchAnswers(_answerSearchString);
        StateHasChanged();
      });
    }

    private async Task SearchAnswers(string searchTerm)
    {
      if (string.IsNullOrWhiteSpace(searchTerm))
      {
        await LoadAnswers(); // Load all answers if search term is empty
      }
      else
      {
        searchTerm = searchTerm.ToLower(); // Convert search term to lowercase
        answers = await appDbContext.Set<AnswerModel>()
            .Where(a => a.Id.ToString().Contains(searchTerm) ||
                        a.AnswerText.ToLower().Contains(searchTerm) ||
                        a.Points.ToString().Contains(searchTerm) ||
                        a.AnswerType.ToString().ToLower().Contains(searchTerm))
            .ToListAsync();
      }
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
  }
}