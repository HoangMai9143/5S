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
    private HashSet<int> existingQuestionIds = new HashSet<int>();
    private HashSet<int> originalExistingQuestionIds = new HashSet<int>();

    private string _searchString = string.Empty;
    private string _questionSearchString = string.Empty;
    private SurveyModel selectedSurvey;

    protected override async Task OnInitializedAsync()
    {
      await LoadSurveys();
      await LoadQuestions();
    }

    private async Task LoadSurveys() // Load all surveys
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

    private async Task LoadQuestions() // Load all questions
    {
      try
      {
        // Modified to include OrderByDescending for Id
        questions = await appDbContext.Set<QuestionModel>().OrderByDescending(q => q.Id).ToListAsync();
        if (selectedSurvey != null)
        {
          await LoadExistingQuestions();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading questions: {ex.Message}");
        questions = new List<QuestionModel>();
      }
    }


    private async Task LoadExistingQuestions()
    {
      var existingQuestionIdsList = await appDbContext.Set<SurveyQuestionModel>()
          .Where(sq => sq.SurveyId == selectedSurvey.Id)
          .Select(sq => sq.QuestionId)
          .ToListAsync();

      // Convert to hashset for faster lookup
      existingQuestionIds = new HashSet<int>(existingQuestionIdsList);
      originalExistingQuestionIds = new HashSet<int>(existingQuestionIds);
      selectedQuestions = new HashSet<QuestionModel>(questions.Where(q => existingQuestionIds.Contains(q.Id)));
      StateHasChanged();
    }

    // Filter surveys
    private Func<SurveyModel, bool> _surveyQuickFilter => x =>
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

    // Filter questions
    private Func<QuestionModel, bool> _questionQuickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_questionSearchString))
        return true;

      if (x.Id.ToString().Contains(_questionSearchString))
        return true;

      if (x.QuestionContext.Contains(_questionSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    };


    private async Task HandleTabChanged(int index)
    {
      if (index == 1 && selectedSurvey == null)
      {
        dialogService.Show<AlertDialog>("Please select a survey first.");
        return;
      }
      if (index == 0 && selectedSurvey != null)
      {
        selectedSurvey = null;
        selectedQuestions.Clear();
      }
      if (selectedSurvey != null)
      {
        await LoadExistingQuestions();
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
    private async Task SaveSurveyQuestions()
    {
      try
      {
        // Get current and existing question IDs
        var currentQuestionIds = selectedQuestions.Select(q => q.Id).ToHashSet();

        // Determine questions to remove and add
        var questionsToRemove = existingQuestionIds.Except(currentQuestionIds).ToList();
        var questionsToAdd = currentQuestionIds.Except(existingQuestionIds).ToList();

        // Update database
        foreach (var questionId in questionsToRemove)
        {
          var surveyQuestion = await appDbContext.Set<SurveyQuestionModel>()
              .FirstOrDefaultAsync(sq => sq.SurveyId == selectedSurvey.Id && sq.QuestionId == questionId);
          if (surveyQuestion != null)
            appDbContext.Set<SurveyQuestionModel>().Remove(surveyQuestion);
        }

        await appDbContext.Set<SurveyQuestionModel>().AddRangeAsync(
            questionsToAdd.Select(questionId => new SurveyQuestionModel { SurveyId = selectedSurvey.Id, QuestionId = questionId })
        );

        await appDbContext.SaveChangesAsync();

        // Update existingQuestionIds to reflect the changes
        existingQuestionIds = new HashSet<int>(currentQuestionIds);
        originalExistingQuestionIds = new HashSet<int>(existingQuestionIds);

        snackbar.Add("Survey questions saved to database successfully.", Severity.Success);
      }
      catch (Exception ex)
      {
        snackbar.Add($"Error saving survey questions: {ex.Message}", Severity.Error);
      }
      finally
      {
        StateHasChanged();
      }
    }

    private void OnSelectionChanged(HashSet<QuestionModel> selectedItems)
    {
      selectedQuestions = selectedItems;
      StateHasChanged();
    }
    private void OnSelectSurvey(SurveyModel survey)
    {
      selectedSurvey = survey;
      LoadExistingQuestions();
      activeIndex = 1;
    }
  }
}