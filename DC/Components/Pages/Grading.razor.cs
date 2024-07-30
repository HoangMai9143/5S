using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Components.Dialog;
using DC.Models;
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
        surveys = await appDbContext.SurveyModel.ToListAsync();
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
      var dialog = await dialogService.ShowAsync<StaffGradingDialog>("Grade Staff", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled)
      {
        // Handle the grading result, e.g., save to database
        await SaveGradingResult(result.Data as List<QuestionAnswerModel>);
      }
    }

    private async Task SaveGradingResult(List<QuestionAnswerModel> gradingResult)
    {
      if (gradingResult != null)
      {
        appDbContext.QuestionAnswerModel.AddRange(gradingResult);
        await appDbContext.SaveChangesAsync();
        sb.Add("Grading saved successfully", Severity.Success);
      }
    }
  }
}