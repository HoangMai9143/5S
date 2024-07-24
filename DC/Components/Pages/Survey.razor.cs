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
            .OrderByDescending(s => s.Id)
            .ToListAsync();

        // Update IsActive property for surveys if the end date has passed
        bool anyChanges = false;
        foreach (var survey in surveys)
        {
          if (survey.EndDate < DateTime.Today.AddDays(-1) && survey.IsActive)
          {
            survey.IsActive = false; // Mark the survey as ended
            anyChanges = true;
          }
        }

        if (anyChanges)
        {
          await appDbContext.SaveChangesAsync(); // Save changes if any survey's status was updated
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading surveys: {ex.Message}");
        sb.Add("Error loading surveys", Severity.Error);
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
        Console.WriteLine($"Error loading question(s): {ex.Message}");
        sb.Add("Error loading question(s)", Severity.Error);
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




    private async Task HandleTabChanged(int index)
    {
      if (index == 1 && selectedSurvey == null)
      {
        sb.Add("Please select a survey first.", Severity.Warning);

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

    private async Task OpenCreateSurveyDialog()
    {
      var dialog = dialogService.Show<SurveyCreateDialog>("Create New Survey");
      var result = await dialog.Result;

      if (!result.Canceled && result.Data is SurveyModel newSurvey)
      {
        await LoadSurveys();
        sb.Add($"Survey created successfully with ID: {newSurvey.Id}.", Severity.Success);
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

        sb.Add("Survey question(s) saved to database successfully.", Severity.Success);
      }
      catch (Exception ex)
      {
        sb.Add($"Error saving survey question(s): {ex.Message}", Severity.Error);
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
    private async Task OpenEditSurveyDialog(SurveyModel surveyToEdit)
    {
      var parameters = new DialogParameters
    {
        { "Survey", surveyToEdit }
    };

      var options = new DialogOptions()
      {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        Position = DialogPosition.Center,
        CloseOnEscapeKey = true,
        FullScreen = false,
      };

      var dialog = await dialogService.ShowAsync<SurveyEditDialog>("Edit Survey", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled)
      {
        await LoadSurveys();
        sb.Add($"Survey {surveyToEdit.Id} updated successfully.", Severity.Success);
      }
    }
    private async Task DeleteSurvey(SurveyModel surveyToDelete)
    {
      var parameters = new DialogParameters
    {
        { "ContentText", "Are you sure you want to delete this survey?" },
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

      var dialog = await dialogService.ShowAsync<ConfirmDialog>("Delete Survey", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled)
      {
        await ConfirmedDeleteSurvey(surveyToDelete);
        sb.Add($"Survey {surveyToDelete.Id} deleted successfully.", Severity.Success);
      }
    }
    private async Task ConfirmedDeleteSurvey(SurveyModel surveyToDelete)
    {
      int deletedSurveyId = surveyToDelete.Id;
      appDbContext.Set<SurveyModel>().Remove(surveyToDelete);
      await appDbContext.SaveChangesAsync();
      await LoadSurveys();
    }

    private async Task CloneSurvey(SurveyModel surveyToClone)
    {
      var clonedSurvey = new SurveyModel
      {
        StartDate = surveyToClone.StartDate,
        EndDate = surveyToClone.EndDate,
        CreatedDate = DateTime.Now
      };

      await appDbContext.Set<SurveyModel>().AddAsync(clonedSurvey);
      await appDbContext.SaveChangesAsync();

      var surveyQuestions = await appDbContext.Set<SurveyQuestionModel>()
          .Where(sq => sq.SurveyId == surveyToClone.Id)
          .ToListAsync();

      foreach (var question in surveyQuestions)
      {
        await appDbContext.Set<SurveyQuestionModel>().AddAsync(new SurveyQuestionModel
        {
          SurveyId = clonedSurvey.Id,
          QuestionId = question.QuestionId
        });
      }

      await appDbContext.SaveChangesAsync();
      await LoadSurveys();
      sb.Add($"Survey {surveyToClone.Id} cloned successfully with ID: {clonedSurvey.Id}", Severity.Success);
    }
  }
}