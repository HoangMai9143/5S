using DC.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class SurveyCreateDialog
  {

    [CascadingParameter] MudDialogInstance mudDialog { get; set; }

    private DateRange _dateRange = new DateRange(DateTime.Today, DateTime.Today);
    private string _title = string.Empty;

    private void Cancel()
    {
      mudDialog.Cancel();
    }

    private async Task Submit()
    {
      if (_dateRange.Start.HasValue || _dateRange.End.HasValue)
      {
        var startDate = _dateRange.Start.Value;
        var endDate = _dateRange.End.HasValue ? _dateRange.End.Value : startDate;

        if (startDate > endDate)
        {
          sb.Add("End date must be after or equal to start date", Severity.Error);
          return;
        }

        var newSurvey = new SurveyModel
        {
          Title = _title,
          StartDate = startDate,
          EndDate = endDate,
          CreatedDate = DateTime.UtcNow,
          IsActive = true
        };

        // Edit
        // if (_title == string.Empty)
        if (string.IsNullOrEmpty(_title))
        {
          sb.Add("Title can't be empty", Severity.Warning);
          return;
        }
        try
        {
          await appDbContext.Set<SurveyModel>().AddAsync(newSurvey);
          await appDbContext.SaveChangesAsync();
          mudDialog.Close(DialogResult.Ok(newSurvey));
        }
        catch (Exception ex)
        {
          sb.Add($"Error creating survey: {ex.Message}", Severity.Error);
        }
      }
      else
      {
        sb.Add("Please select a start date", Severity.Warning);
      }
    }
  }
}