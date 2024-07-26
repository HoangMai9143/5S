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
    private QuestionModel currentQuestion = new QuestionModel();
    private List<AnswerModel> currentAnswers = new List<AnswerModel>();
    private string _searchString = string.Empty;

    private System.Timers.Timer _questionDebounceTimer;
    private const int DebounceDelay = 300; // milliseconds

    private int selectedAnswerIndex = -1;
    private bool isLoading = true;


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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        await LoadQuestions();
        _questionDebounceTimer = new System.Timers.Timer(DebounceDelay);
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
        questions = await appDbContext.Set<QuestionModel>()
            .Include(q => q.Answers)
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
          AnswerType = AnswerType.SingleChoice // Default to single choice
        };
        await appDbContext.Set<QuestionModel>().AddAsync(newQuestion);
        await appDbContext.SaveChangesAsync();

        await LoadQuestions();

        _searchString = string.Empty;
        sb.Add($"Question added successfully with ID: {newQuestion.Id}", Severity.Success);
        StateHasChanged();
      }
    }

    private async Task HandleTabChanged(int index)
    {
      if (index == 1 && currentQuestion.Id == 0)
      {
        sb.Add("Please select a question first.", Severity.Warning);
        return;
      }
      activeIndex = index;
    }

    private async Task SaveQuestion()
    {
      try
      {
        if (currentQuestion.Id == 0)
        {
          await appDbContext.Set<QuestionModel>().AddAsync(currentQuestion);
        }
        else
        {
          appDbContext.QuestionModel.Update(currentQuestion);
        }

        await appDbContext.SaveChangesAsync();

        // Remove answers that are no longer in the list
        var existingAnswers = await appDbContext.AnswerModel
            .Where(a => a.QuestionId == currentQuestion.Id)
            .ToListAsync();

        foreach (var existingAnswer in existingAnswers)
        {
          if (!currentAnswers.Exists(a => a.Id == existingAnswer.Id))
          {
            appDbContext.AnswerModel.Remove(existingAnswer);
          }
        }

        // Add or update current answers
        foreach (var answer in currentAnswers)
        {
          answer.QuestionId = currentQuestion.Id;
          if (answer.Id == 0)
          {
            await appDbContext.AnswerModel.AddAsync(answer);
          }
          else
          {
            appDbContext.AnswerModel.Update(answer);
          }
        }

        await appDbContext.SaveChangesAsync();
        sb.Add("Saved successfully.", Severity.Success);
        await LoadQuestions();

        // Reset current question and answers
        currentQuestion = new QuestionModel();
        currentAnswers.Clear(); // Simplified collection initialization

        // Go back to the question list
        activeIndex = 0;
      }
      catch (Exception ex)
      {
        sb.Add($"Error saving question: {ex.Message}", Severity.Error);
      }
    }

    private void OnMultipleChoiceChanged(AnswerModel changedAnswer)
    {
      StateHasChanged();
    }

    private void AddNewAnswer()
    {
      currentAnswers.Add(new AnswerModel
      {
        QuestionId = currentQuestion.Id,
        Points = 1
      });
    }

    private void RemoveAnswer(AnswerModel answer)
    {
      currentAnswers.Remove(answer);
      if (answer.Id != 0)
      {
        appDbContext.AnswerModel.Remove(answer);
      }
    }

    private async Task LoadQuestion(int questionId)
    {
      currentQuestion = await appDbContext.QuestionModel
          .Include(q => q.Answers)
          .FirstOrDefaultAsync(q => q.Id == questionId);

      if (currentQuestion != null)
      {
        currentAnswers = currentQuestion.Answers.ToList();
        UpdateSelectedAnswerIndex();
      }
      else
      {
        currentQuestion = new QuestionModel();
        currentAnswers = new List<AnswerModel>();
      }
      activeIndex = 1;
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
        await LoadQuestions();
      }
      else
      {
        searchTerm = searchTerm.ToLower();
        questions = await appDbContext.Set<QuestionModel>()
            .Where(q => q.Id.ToString().Contains(searchTerm) ||
                        q.QuestionContext.ToLower().Contains(searchTerm))
            .OrderByDescending(q => q.Id)
            .ToListAsync();
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
        QuestionContext = questionToClone.QuestionContext,
        AnswerType = questionToClone.AnswerType
      };

      await appDbContext.Set<QuestionModel>().AddAsync(clonedQuestion);
      await appDbContext.SaveChangesAsync();

      foreach (var answer in questionToClone.Answers)
      {
        var clonedAnswer = new AnswerModel
        {
          AnswerText = answer.AnswerText,
          IsCorrect = answer.IsCorrect,
          Points = answer.Points,
          QuestionId = clonedQuestion.Id
        };
        await appDbContext.Set<AnswerModel>().AddAsync(clonedAnswer);
      }

      await appDbContext.SaveChangesAsync();
      await LoadQuestions();
      sb.Add($"Question {questionToClone.Id} cloned successfully with ID: {clonedQuestion.Id}", Severity.Success);
    }
    protected override void OnParametersSet()
    {
      base.OnParametersSet();
      UpdateSelectedAnswerIndex();
    }

    private void UpdateSelectedAnswerIndex()
    {
      selectedAnswerIndex = currentAnswers.FindIndex(a => a.IsCorrect);
    }

    private void OnSelectedAnswerIndexChanged(int index)
    {
      selectedAnswerIndex = index;
      if (index >= 0 && index < currentAnswers.Count)
      {
        for (int i = 0; i < currentAnswers.Count; i++)
        {
          currentAnswers[i].IsCorrect = (i == index);
        }
      }
      StateHasChanged();
    }
  }
}