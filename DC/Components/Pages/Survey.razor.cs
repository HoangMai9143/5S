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
  public partial class Survey
  {
    private int activeIndex = 0;
    private List<SurveyModel> surveys = new List<SurveyModel>();
    private string _searchString = string.Empty;

    private SurveyModel selectedSurvey;

    protected override async Task OnInitializedAsync()
    {
      await LoadSurveys();
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

    private void HandleTabChanged(int index)
    {
      if (index == 1 && selectedSurvey == null) // Assuming index 1 is for "Choose Question" tab
      {
        dialogService.Show<AlertDialog>("Please select a survey first.");
        return;
      }
      activeIndex = index;
    }

    private async Task OpenSurveyDialog()
    {
      var dialog = dialogService.Show<SurveyDialog>("Create New Survey");
      var result = await dialog.Result;

      if (!result.Canceled)
      {
        await LoadSurveys();
      }
    }
    private void SelectItem(SurveyModel survey)
    {
      selectedSurvey = survey;
    }
  }
}