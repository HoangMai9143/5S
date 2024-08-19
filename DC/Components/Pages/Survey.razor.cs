using Microsoft.EntityFrameworkCore;

using DC.Components.Dialog;
using DC.Models;

using MudBlazor;

namespace DC.Components.Pages
{
  public partial class Survey
  {
    private List<SurveyModel> surveyList = new List<SurveyModel>(); // List of surveys to display
    private List<QuestionModel> questionsList = new List<QuestionModel>(); // List of questions to display

    private HashSet<QuestionModel> currentSelectedQuestion = new HashSet<QuestionModel>(); // Holds the questions that are currently selected by the user.
    private HashSet<int> savedQuestionId = new HashSet<int>(); // The questions that are already saved in database
    private HashSet<int> initialLoadedQuestionId = new HashSet<int>(); // Initial questions loaded from the database (to compare with currentSelectedQuestion)

    private string _surveySearchString = string.Empty; // Search string for surveys
    private string _questionSearchString = string.Empty; // Search string for questions
    private SurveyModel _selectedSurvey; // The survey that is currently selected by the user

    private System.Timers.Timer _surveySearchDebounceTimer; // Timer to debounce survey search input
    private System.Timers.Timer _questionSearchDebounceTimer; // Timer to debounce question search input
    private const int DebounceDelay = 300; // milliseconds
    private bool isLoading = true;


    //* 1. Initialization and Loading
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        await LoadSurveys();
        await LoadQuestions();

        _surveySearchDebounceTimer = new System.Timers.Timer(DebounceDelay);
        _surveySearchDebounceTimer.Elapsed += async (sender, e) => await SurveyDebounceTimerElapsed();
        _surveySearchDebounceTimer.AutoReset = false;

        _questionSearchDebounceTimer = new System.Timers.Timer(DebounceDelay);
        _questionSearchDebounceTimer.Elapsed += async (sender, e) => await QuestionDebounceTimerElapsed();
        _questionSearchDebounceTimer.AutoReset = false;

        isLoading = false;
        StateHasChanged();
      }
    }

    private async Task LoadSurveys()
    {
      try
      {
        surveyList = await appDbContext.Set<SurveyModel>()
            .OrderByDescending(s => s.Id)
            .ToListAsync();

        // Update IsActive property for surveys if the end date has passed
        bool anyChanges = false;
        foreach (var survey in surveyList)
        {
          if (survey.EndDate < DateTime.Today && survey.IsActive)
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
        sb.Add("Error loading surveys, reloading page...", Severity.Error);
        surveyList = new List<SurveyModel>();
        await Task.Delay(1000);
        await LoadSurveys();
      }
    }

    private async Task LoadQuestions()
    {
      try
      {
        questionsList = await appDbContext.Set<QuestionModel>()
            .OrderByDescending(q => q.Id)
            .ToListAsync();
        if (_selectedSurvey != null)
        {
          await GetExistingQuestions();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading question(s): {ex.Message}");
        sb.Add("Error loading question(s), reloading page...", Severity.Error);
        questionsList = new List<QuestionModel>();
        await Task.Delay(1000);
        await LoadQuestions();
      }
    }

    //* Compare saved questions with the current selected questions
    private async Task GetExistingQuestions()
    {
      var existingQuestionIdsList = await appDbContext.Set<SurveyQuestionModel>()
          .Where(sq => sq.SurveyId == _selectedSurvey.Id)
          .Select(sq => sq.QuestionId)
          .ToListAsync();

      // Convert to hashset for faster lookup
      savedQuestionId = new HashSet<int>(existingQuestionIdsList);
      initialLoadedQuestionId = new HashSet<int>(savedQuestionId);
      currentSelectedQuestion = new HashSet<QuestionModel>(questionsList.Where(q => savedQuestionId.Contains(q.Id)));
      StateHasChanged();
    }

    //* 2. Survey-related functions
    //* Save selected surveys to the database
    private async Task SaveSurveyQuestions()
    {
      try
      {
        // Get current and existing question IDs
        var currentQuestionIds = currentSelectedQuestion.Select(q => q.Id).ToHashSet();

        // Determine questions to remove and add
        var questionsToRemove = savedQuestionId.Except(currentQuestionIds).ToList();
        var questionsToAdd = currentQuestionIds.Except(savedQuestionId).ToList();

        // Update database
        foreach (var questionId in questionsToRemove)
        {
          var surveyQuestion = await appDbContext.Set<SurveyQuestionModel>()
              .FirstOrDefaultAsync(sq => sq.SurveyId == _selectedSurvey.Id && sq.QuestionId == questionId);
          if (surveyQuestion != null)
            appDbContext.Set<SurveyQuestionModel>().Remove(surveyQuestion);
        }

        await appDbContext.Set<SurveyQuestionModel>().AddRangeAsync(
            questionsToAdd.Select(questionId => new SurveyQuestionModel { SurveyId = _selectedSurvey.Id, QuestionId = questionId })
        );

        await appDbContext.SaveChangesAsync();

        // Update existingQuestionIds to reflect the changes
        savedQuestionId = new HashSet<int>(currentQuestionIds);
        initialLoadedQuestionId = new HashSet<int>(savedQuestionId);

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
      int deletedSurveyId = surveyToDelete.Id;
      appDbContext.Set<SurveyModel>().Remove(surveyToDelete);
      await appDbContext.SaveChangesAsync();
      await LoadSurveys();
    }

    //* Clone the survey and it's related questions then open the survey edit dialog (SurveyEditDialog)
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

    //* 3. Question-related functions
    private void OnSelectionChanged(HashSet<QuestionModel> selectedItems)
    {
      currentSelectedQuestion = selectedItems;
      StateHasChanged();
    }

    //* 4. Search and Filter functions
    private Func<SurveyModel, bool> surveyQuickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_surveySearchString))
        return true;

      if (x.Id.ToString().Contains(_surveySearchString, StringComparison.OrdinalIgnoreCase))
        return true;
      if (x.Title.Contains(_surveySearchString, StringComparison.OrdinalIgnoreCase))
        return true;
      if (x.StartDate.ToString("dd/MM/yyyy HH:mm").Contains(_surveySearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.EndDate.ToString("dd/MM/yyyy HH:mm").Contains(_surveySearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.CreatedDate.ToString("dd/MM/yyyy HH:mm").Contains(_surveySearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.IsActive.ToString().Contains(_surveySearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    };

    //* Search surveys by ID, start date, end date, created date, and active status
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

        surveyList = allSurveys.Where(s =>
            s.Id.ToString().Contains(searchTerm) ||
            s.StartDate.ToString("dd/MM/yyyy HH:mm").ToLower().Contains(searchTerm) ||
            s.EndDate.ToString("dd/MM/yyyy HH:mm").ToLower().Contains(searchTerm) ||
            s.CreatedDate.ToString("dd/MM/yyyy HH:mm").ToLower().Contains(searchTerm) ||
            s.IsActive.ToString().ToLower().Contains(searchTerm)
        ).ToList();
      }
    }

    //* Search questions by ID or context
    private async Task SearchQuestions(string searchTerm)
    {
      if (string.IsNullOrWhiteSpace(searchTerm))
      {
        await LoadQuestions();
      }
      else
      {
        searchTerm = searchTerm.ToLower();
        questionsList = await appDbContext.Set<QuestionModel>()
            .Where(q => q.Id.ToString().Contains(searchTerm) ||
                        q.QuestionContext.ToLower().Contains(searchTerm))
            .OrderByDescending(q => q.Id)
            .ToListAsync();
      }
    }

    private async Task OnSurveySearchInput(string value)
    {
      _surveySearchString = value;
      _surveySearchDebounceTimer.Stop();
      _surveySearchDebounceTimer.Start();
    }

    private async Task SurveyDebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchSurveys(_surveySearchString);
        StateHasChanged();
      });
    }

    //* Question debounce timer 
    private async Task OnQuestionSearchInput(string value)
    {
      _questionSearchString = value;
      _questionSearchDebounceTimer.Stop();
      _questionSearchDebounceTimer.Start();
    }

    //* Debounce timer for question search
    private async Task QuestionDebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchQuestions(_questionSearchString);
        StateHasChanged();
      });
    }

    //* 5. Dialog functions
    private async Task OpenCreateSurveyDialog()
    {
      var parameters = new DialogParameters();
      var options = new DialogOptions()
      {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        Position = DialogPosition.Center,
        CloseOnEscapeKey = true,
        FullScreen = false,
      };
      var dialog = await dialogService.ShowAsync<SurveyCreateDialog>("Create Survey", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled && result.Data is SurveyModel newSurvey)
      {
        await LoadSurveys();
        sb.Add($"Survey created successfully with ID: {newSurvey.Id}.", Severity.Success);
        StateHasChanged();
      }
    }

    //* Open the survey edit dialog (SurveyEditDialog)
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

    private async Task OpenChooseQuestionsDialog(SurveyModel survey)
    {
      _selectedSurvey = survey;
      await GetExistingQuestions();

      var parameters = new DialogParameters
        {
            { "Survey", _selectedSurvey },
            { "Questions", questionsList },
            { "SelectedQuestions", currentSelectedQuestion },
            { "ExistingQuestionIds", savedQuestionId }
        };

      var options = new DialogOptions { FullScreen = true, CloseOnEscapeKey = true, CloseButton = true };

      var dialog = await dialogService.ShowAsync<ChooseQuestionsDialog>("Choose Questions", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled && result.Data is HashSet<QuestionModel> updatedSelectedQuestions)
      {
        currentSelectedQuestion = updatedSelectedQuestions;
        await SaveSurveyQuestions();
      }
    }

    //* Open the delete confirm dialog (ConfirmDialog)
    private async Task OpenDeleteConfirmDialog(SurveyModel surveyToDelete)
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
        await DeleteSurvey(surveyToDelete);
        sb.Add($"Survey {surveyToDelete.Id} deleted successfully.", Severity.Success);
      }
    }

    //* 6. Event handlers and Utility functions
    private void OnSelectSurvey(SurveyModel survey)
    {
      _selectedSurvey = survey;
      GetExistingQuestions();
    }
  }
}