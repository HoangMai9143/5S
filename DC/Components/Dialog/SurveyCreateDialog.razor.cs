using Microsoft.AspNetCore.Components;

using DC.Models;

using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class SurveyCreateDialog
  {

    [CascadingParameter] MudDialogInstance mudDialog { get; set; }

    private DateRange dateRange = new DateRange(DateTime.Today, DateTime.Today);
    private string title = string.Empty;

    private void Cancel()
    {
      mudDialog.Cancel();
    }

    private async Task Submit()
    {
      if (dateRange.Start.HasValue || dateRange.End.HasValue)
      {
        var startDate = dateRange.Start.Value;
        var endDate = dateRange.End.HasValue ? dateRange.End.Value : startDate;

        if (startDate > endDate)
        {
          sb.Add("End date must be after or equal to start date", Severity.Error);
          return;
        }

        var newSurvey = new SurveyModel
        {
          Title = title,
          StartDate = startDate,
          EndDate = endDate,
          CreatedDate = DateTime.Now,
          IsActive = true
        };

        // Edit
        // if (title == string.Empty)
        if (string.IsNullOrEmpty(title))
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