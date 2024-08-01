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
    private List<SurveyModel> surveys = new List<SurveyModel>();
    private List<QuestionModel> questions = new List<QuestionModel>();
    private HashSet<QuestionModel> selectedQuestions = new HashSet<QuestionModel>();
    private HashSet<int> existingQuestionIds = new HashSet<int>();
    private HashSet<int> originalExistingQuestionIds = new HashSet<int>();

    private string _searchString = string.Empty;
    private string _questionSearchString = string.Empty;
    private SurveyModel selectedSurvey;

    private System.Timers.Timer _surveyDebounceTimer;
    private System.Timers.Timer _questionDebounceTimer;
    private const int DebounceDelay = 300; // milliseconds
    private bool isLoading = true;


    //* Filter function
    private Func<SurveyModel, bool> _surveyQuickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_searchString))
        return true;

      if (x.Id.ToString().Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;
      if (x.Title.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
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

      if (x.Id.ToString().Contains(_questionSearchString))
        return true;

      if (x.QuestionContext.Contains(_questionSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    };

    //* Initialize
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        await LoadSurveys();
        await LoadQuestions();

        _surveyDebounceTimer = new System.Timers.Timer(DebounceDelay);
        _surveyDebounceTimer.Elapsed += async (sender, e) => await SurveyDebounceTimerElapsed();
        _surveyDebounceTimer.AutoReset = false;

        _questionDebounceTimer = new System.Timers.Timer(DebounceDelay);
        _questionDebounceTimer.Elapsed += async (sender, e) => await QuestionDebounceTimerElapsed();
        _questionDebounceTimer.AutoReset = false;

        isLoading = false;
        StateHasChanged();
      }
    }


    //* Survey CRUD
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
          await appDbContext.SaveChangesAsync();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading surveys: {ex.Message}");
        sb.Add("Error loading surveys, please reload page!", Severity.Error);
        surveys = new List<SurveyModel>();
      }
    }

    private async Task LoadQuestions()
    {
      try
      {
        questions = await appDbContext.Set<QuestionModel>()
            .OrderByDescending(q => q.Id)
            .ToListAsync();
        if (selectedSurvey != null)
        {
          await LoadExistingQuestions();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading question(s): {ex.Message}");
        sb.Add("Error loading question(s), please reload page!", Severity.Error);
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

    //* Dialog functions
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
    private async Task OpenEditSurveyDialog(SurveyModel surveyToEdit)
    {
      var parameters = new DialogParameters { { "Survey", surveyToEdit } };

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
        StateHasChanged();
      }
    }

    private async Task ConfirmedDeleteSurvey(SurveyModel surveyToDelete)
    {
      int deletedSurveyId = surveyToDelete.Id;
      appDbContext.Set<SurveyModel>().Remove(surveyToDelete);
      await appDbContext.SaveChangesAsync();
      await LoadSurveys();
    }
    private async Task OpenChooseQuestionsDialog(SurveyModel survey)
    {
      selectedSurvey = survey;
      await LoadExistingQuestions();

      var parameters = new DialogParameters
        {
            { "Survey", selectedSurvey },
            { "Questions", questions },
            { "SelectedQuestions", selectedQuestions },
            { "ExistingQuestionIds", existingQuestionIds }
        };

      var options = new DialogOptions { FullScreen = true, CloseOnEscapeKey = true, CloseButton = true };

      var dialog = await dialogService.ShowAsync<ChooseQuestionsDialog>("Choose Questions", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled && result.Data is HashSet<QuestionModel> updatedSelectedQuestions)
      {
        selectedQuestions = updatedSelectedQuestions;
        await SaveSurveyQuestions();
      }
    }
    //* Survey CRUD
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

    private async Task SearchSurveys(string searchTerm)
    {
      if (string.IsNullOrWhiteSpace(searchTerm))
      {
        await LoadSurveys(); // Load all surveys if search term is empty
      }
      else
      {
        searchTerm = searchTerm.ToLower(); // Convert search term to lowercase
        var allSurveys = await appDbContext.Set<SurveyModel>()
            .OrderByDescending(s => s.Id)
            .ToListAsync();

        surveys = allSurveys.Where(s =>
            s.Id.ToString().Contains(searchTerm) ||
            s.StartDate.ToString("dd/MM/yyyy HH:mm").ToLower().Contains(searchTerm) ||
            s.EndDate.ToString("dd/MM/yyyy HH:mm").ToLower().Contains(searchTerm) ||
            s.CreatedDate.ToString("dd/MM/yyyy HH:mm").ToLower().Contains(searchTerm) ||
            s.IsActive.ToString().ToLower().Contains(searchTerm)
        ).ToList();
      }
    }
    //* Event handlers
    private void OnSelectionChanged(HashSet<QuestionModel> selectedItems)
    {
      selectedQuestions = selectedItems;
      StateHasChanged();
    }
    private void OnSelectSurvey(SurveyModel survey)
    {
      selectedSurvey = survey;
      LoadExistingQuestions();
    }

    private async Task CloneSurvey(SurveyModel surveyToClone)
    {
      var clonedSurvey = new SurveyModel
      {
        Title = surveyToClone.Title,
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
      OpenEditSurveyDialog(clonedSurvey);
    }

    private async Task OnSurveySearchInput(string value)
    {
      _searchString = value;
      _surveyDebounceTimer.Stop();
      _surveyDebounceTimer.Start();
    }

    private async Task SurveyDebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchSurveys(_searchString);
        StateHasChanged();
      });
    }


    private async Task OnQuestionSearchInput(string value)
    {
      _questionSearchString = value;
      _questionDebounceTimer.Stop();
      _questionDebounceTimer.Start();
    }

    private async Task QuestionDebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchQuestions(_questionSearchString);
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

  }
}