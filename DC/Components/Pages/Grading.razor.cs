using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Components.Dialog;
using DC.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace DC.Components.Pages
{
  public partial class Grading
  {
    private bool isLoading = true;
    private int activeIndex = 0;
    private List<SurveyModel> surveys = new List<SurveyModel>();
    private SurveyModel selectedSurvey;
    private Dictionary<string, List<StaffModel>> staffByDepartment = new Dictionary<string, List<StaffModel>>();
    private Dictionary<int, double> staffScores = new Dictionary<int, double>();
    private string _searchString = "";
    private Func<SurveyModel, bool> _surveyQuickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_searchString))
        return true;

      if (x.Title.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.Id.ToString().Contains(_searchString))
        return true;

      return false;
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        await LoadSurveys();
        await LoadStaff();
        isLoading = false;
        StateHasChanged();
      }
    }

    private async Task LoadSurveys()
    {
      try
      {
        surveys = await appDbContext.SurveyModel
            .OrderByDescending(s => s.Id)
            .ToListAsync();
      }
      catch (Exception ex)
      {
        sb.Add("Error Loading Surveys", Severity.Error);
      }
    }

    private async Task LoadStaff()
    {
      try
      {
        var allStaff = await appDbContext.StaffModel.Where(s => s.IsActive).ToListAsync();
        staffByDepartment = allStaff.GroupBy(s => s.Department ?? "Unassigned")
                                    .ToDictionary(g => g.Key, g => g.ToList());

        if (selectedSurvey != null)
        {
          // Load scores
          var scores = await appDbContext.SurveyResultModel
              .Where(sr => sr.SurveyId == selectedSurvey.Id)
              .ToListAsync();

          staffScores = scores.ToDictionary(sr => sr.StaffId, sr => sr.FinalGrade);
        }
        else
        {
          staffScores.Clear();
        }
      }
      catch (Exception ex)
      {
        sb.Add("Error Loading Staffs", Severity.Error);
      }
    }

    private void HandleTabChanged(int index)
    {
      if (index == 1 && selectedSurvey == null)
      {
        sb.Add("Please select a survey first", Severity.Error);
        return;
      }
      activeIndex = index;
    }

    private void OnSurveySearchInput(string value)
    {
      _searchString = value;
    }

    private void OnSelectSurvey(SurveyModel survey)
    {
      selectedSurvey = survey;
      activeIndex = 1; // Switch to the second tab
      StateHasChanged();
    }

    private async Task OpenStaffGradingDialog(StaffModel staff)
    {
      var parameters = new DialogParameters
      {
        ["Staff"] = staff,
        ["Survey"] = selectedSurvey
      };

      var options = new DialogOptions { FullScreen = true, CloseButton = true };
      var dialog = await dialogService.ShowAsync<GradingDialog>("Grade Staff", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled)
      {
        var dialogResult = (dynamic)result.Data;
        await SaveGradingResult(dialogResult.GradingResult, dialogResult.Changes);
        await CalculateAndSaveScores(selectedSurvey.Id);
      }
    }

    private async Task SaveGradingResult(List<QuestionAnswerModel> gradingResult, bool changes)
    {
      if (!changes)
      {
        sb.Add("No changes were made", Severity.Info);
        return;
      }
      if (changes)
      {
        if (gradingResult != null && gradingResult.Any())
        {
          appDbContext.QuestionAnswerModel.AddRange(gradingResult);
        }

        try
        {
          await appDbContext.SaveChangesAsync();
          sb.Add("Changes saved successfully", Severity.Success);

          await LoadStaff(); // Refresh staffScores after saving
          StateHasChanged(); // Update the UI to reflect the changes
        }
        catch (DbUpdateException ex)
        {
          Console.WriteLine(ex.ToString());

          if (ex.InnerException is SqlException sqlEx)
          {
            switch (sqlEx.Number)
            {
              case 547:
                sb.Add("One or more answers are no longer valid. Please refresh and try again.", Severity.Error);
                break;
              default:
                sb.Add($"Database error occurred: {sqlEx.Message}", Severity.Error);
                break;
            }
          }
          else
          {
            sb.Add("Error saving changes. Please try again.", Severity.Error);
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.ToString());
          sb.Add($"An unexpected error occurred: {ex.Message}", Severity.Error);
        }
      }
      else
      {
        sb.Add("No changes were made", Severity.Info);
      }
    }

    private async Task CalculateAndSaveScores(int surveyId)
    {
      try
      {
        var surveyQuestions = await appDbContext.SurveyQuestionModel
            .Where(sq => sq.SurveyId == surveyId)
            .Select(sq => sq.QuestionId)
            .ToListAsync();

        var totalPossiblePoints = await appDbContext.AnswerModel
            .Where(a => surveyQuestions.Contains(a.QuestionId))
            .SumAsync(a => a.Points);

        var staffAnswerGroups = await appDbContext.QuestionAnswerModel
            .Where(qa => qa.SurveyId == surveyId && qa.Answer != null)
            .Include(qa => qa.Answer)
            .GroupBy(qa => qa.StaffId)
            .ToListAsync();

        var staffScores = new Dictionary<int, double>();

        foreach (var staffGroup in staffAnswerGroups)
        {
          int staffId = staffGroup.Key;
          double totalPoints = staffGroup.Sum(qa => qa.Answer.Points);

          double scorePercentage = totalPossiblePoints > 0 ? (totalPoints / totalPossiblePoints) * 100 : 0;
          staffScores[staffId] = scorePercentage;

          var surveyResult = await appDbContext.SurveyResultModel
              .FirstOrDefaultAsync(r => r.SurveyId == surveyId && r.StaffId == staffId)
              ?? new SurveyResultModel
              {
                SurveyId = surveyId,
                StaffId = staffId
              };

          surveyResult.FinalGrade = scorePercentage;
          appDbContext.Update(surveyResult);
        }

        await appDbContext.SaveChangesAsync();
        sb.Add("Scores calculated and saved successfully", Severity.Success);

        foreach (var kvp in staffScores)
        {
          var staff = await appDbContext.StaffModel.FindAsync(kvp.Key);
          sb.Add($"Staff: {staff?.FullName ?? "Unknown"}, Score: {kvp.Value:F2}%", Severity.Info);
        }
      }
      catch (DbUpdateException ex)
      {
        if (ex.InnerException is SqlException sqlEx)
        {
          HandleSqlException(sqlEx);
        }
        else
        {
          sb.Add("Error saving changes. Please try again.", Severity.Error);
        }
      }
      catch (Exception ex)
      {
        sb.Add($"An error occurred while calculating scores: {ex.Message}", Severity.Error);
      }
    }


    private void HandleSqlException(SqlException sqlEx)
    {
      switch (sqlEx.Number)
      {
        case 547: // Foreign key constraint violation
          sb.Add("One or more answers are no longer valid. Please refresh and try again.", Severity.Error);
          break;
        default:
          sb.Add($"Database error occurred: {sqlEx.Message}", Severity.Error);
          break;
      }
    }
  }
}