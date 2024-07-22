using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Components.Dialog;
using DC.Models;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Microsoft.AspNetCore.Components.Web;

namespace DC.Components.Pages
{
  public partial class Survey
  {
    private int activeIndex = 0;
    private List<SurveyModel> surveys = new List<SurveyModel>();
    private List<QuestionModel> questions = new List<QuestionModel>();
    private HashSet<QuestionModel> selectedQuestions = new HashSet<QuestionModel>();

    private string _searchString = string.Empty;
    private string _questionSearchString = string.Empty;

    private SurveyModel selectedSurvey;

    protected override async Task OnInitializedAsync()
    {
      await LoadSurveys();
      await LoadQuestions();
    }

    private async Task LoadSurveys()
    {
      try
      {
        surveys = await appDbContext.Set<SurveyModel>()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Id)
            .ToListAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading surveys: {ex.Message}");
        surveys = new List<SurveyModel>();
      }
    }

    private async Task LoadQuestions()
    {
      try
      {
        questions = await appDbContext.Set<QuestionModel>().OrderBy(q => q.Id).ToListAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading questions: {ex.Message}");
        questions = new List<QuestionModel>();
      }
    }

    private Func<SurveyModel, bool> _quickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_searchString))
        return true;

      if (x.Id.ToString().Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.StartDate.ToString("dd/MM/yyyy HH:mm").Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.EndDate.ToString("dd/MM/yyyy HH:mm").Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.CreatedDate.ToString("dd/MM/yyyy HH:mm").Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.IsActive.ToString().Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    };

    private Func<QuestionModel, bool> _questionQuickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_questionSearchString))
        return true;

      if (x.Id.ToString().Contains(_questionSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.QuestionContext.Contains(_questionSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    };

    private void HandleTabChanged(int index)
    {
      if (index == 1 && selectedSurvey == null)
      {
        dialogService.Show<AlertDialog>("Please select a survey first.");
        return;
      }
      activeIndex = index;
    }

    private async Task OpenSurveyDialog()
    {
      var dialog = dialogService.Show<SurveyCreateDialog>("Create New Survey");
      var result = await dialog.Result;

      if (!result.Canceled)
      {
        await LoadSurveys();
      }
    }

    private void SelectItem(SurveyModel survey)
    {
      selectedSurvey = survey;
      activeIndex = 1;
    }

    private async Task AddQuestionsToSurvey()
    {
      if (selectedSurvey == null || !selectedQuestions.Any())
      {
        Snackbar.Add("Please select a survey and at least one question.", Severity.Warning);
        return;
      }

      try
      {
        foreach (var question in selectedQuestions)
        {
          var surveyQuestion = new SurveyQuestionModel
          {
            SurveyId = selectedSurvey.Id,
            QuestionId = question.Id
          };

          await appDbContext.Set<SurveyQuestionModel>().AddAsync(surveyQuestion);
        }

        await appDbContext.SaveChangesAsync();

        Snackbar.Add("Questions added to survey successfully.", Severity.Success);
        selectedQuestions.Clear();
      }
      catch (Exception ex)
      {
        Snackbar.Add($"Error adding questions to survey: {ex.Message}", Severity.Error);
      }
    }
  }
}