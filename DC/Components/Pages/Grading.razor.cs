using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
  }
}