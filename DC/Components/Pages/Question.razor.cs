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
    private bool isLoading = true;
    private List<QuestionModel> questions = new();
    private QuestionModel currentQuestion = new();
    private List<AnswerModel> currentAnswers = [];
    private string _searchString = string.Empty;
    private AnswerType _selectedAnswerType = AnswerType.SingleChoice;
    private System.Timers.Timer _questionDebounceTimer;
    private const int DebounceDelay = 300; // milliseconds

    private int selectedAnswerIndex = -1;


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
        sb.Add("Error loading , please reload page!", Severity.Error);
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
          AnswerType = _selectedAnswerType
        };
        await appDbContext.Set<QuestionModel>().AddAsync(newQuestion);
        await appDbContext.SaveChangesAsync();

        await LoadQuestions();

        _searchString = string.Empty;
        sb.Add($"Question added successfully with ID: {newQuestion.Id}", Severity.Success);
        OpenQuestionEditDialog(newQuestion.Id);
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



    private void OnMultipleChoiceChanged(AnswerModel changedAnswer)
    {
      StateHasChanged();
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
      try
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
      catch (Exception ex)
      {
        Console.WriteLine($"Error searching questions: {ex.Message}");
        sb.Add("An error occurred while searching questions. Please try again later.", Severity.Error);
      }
    }

    private async Task DeleteQuestion(QuestionModel questionToDelete)
    {
      try
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
      catch (Exception ex)
      {
        sb.Add($"Can't delete this question, it already been used", Severity.Error);
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
      try
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
            Points = answer.Points,
            QuestionId = clonedQuestion.Id
          };
          await appDbContext.Set<AnswerModel>().AddAsync(clonedAnswer);
        }

        await appDbContext.SaveChangesAsync();
        await LoadQuestions();
        sb.Add($"Question {questionToClone.Id} cloned successfully with ID: {clonedQuestion.Id}", Severity.Success);
      }
      catch (Exception ex)
      {
        sb.Add($"Error cloning question: {ex.Message}", Severity.Error);
      }
    }
    private async Task OpenQuestionEditDialog(int questionId)
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
  }
}