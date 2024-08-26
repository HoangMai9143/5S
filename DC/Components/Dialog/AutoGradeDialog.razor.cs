using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class AutoGradeDialog
  {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    [Parameter] public List<StaffModel> staffList { get; set; }
    [Parameter] public double maxPossibleScore { get; set; }
    [Parameter] public Dictionary<int, double> staffScores { get; set; }

    private List<StaffViewModel> staffViewModels;
    private List<PreviewStaffViewModel> previewStaffViewModels;
    private double minRange;
    private double maxRange;
    private string searchString = "";
    private bool previewMode = false;

    protected override void OnInitialized()
    {
      staffViewModels = staffList.Select(s => new StaffViewModel
      {
        Id = s.Id,
        FullName = s.FullName,
        Department = s.Department,
        CurrentScore = staffScores.TryGetValue(s.Id, out var score) ? score : (double?)null,
        IsSelected = false
      }).ToList();
      minRange = 0;
      maxRange = maxPossibleScore;
    }

    private IEnumerable<StaffViewModel> FilteredStaffList => staffViewModels
        .Where(s => string.IsNullOrWhiteSpace(searchString) ||
                    s.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    s.Department.Contains(searchString, StringComparison.OrdinalIgnoreCase))
        .OrderBy(s => s.FullName);

    private IEnumerable<PreviewStaffViewModel> PreviewStaffList => previewStaffViewModels.OrderBy(s => s.FullName);

    private void SelectAllStaff()
    {
      foreach (var staff in staffViewModels)
      {
        staff.IsSelected = true;
      }
    }

    private void DeselectAllStaff()
    {
      foreach (var staff in staffViewModels)
      {
        staff.IsSelected = false;
      }
    }

    private Color GetPreviewScoreColor(double? currentScore, double newScore)
    {
      if (!currentScore.HasValue) return Color.Success; // New score is always good if there is no current score
      if (newScore > currentScore.Value) return Color.Success;
      if (newScore < currentScore.Value) return Color.Error;
      return Color.Primary;
    }

    private void PreviewAutoGrade()
    {
      var random = new Random();
      previewStaffViewModels = staffViewModels
          .Where(s => s.IsSelected)
          .Select(s => new PreviewStaffViewModel
          {
            Id = s.Id,
            FullName = s.FullName,
            Department = s.Department,
            CurrentScore = s.CurrentScore,
            NewScore = random.NextDouble() * (maxRange - minRange) + minRange
          })
          .ToList();
      previewMode = true;
    }

    private void BackToConfiguration()
    {
      previewMode = false;
    }

    void Submit() => MudDialog.Close(DialogResult.Ok(new { StaffScores = previewStaffViewModels.ToDictionary(s => s.Id, s => s.NewScore) }));
    void Cancel() => MudDialog.Cancel();


  }

  class StaffViewModel
  {
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Department { get; set; }
    public double? CurrentScore { get; set; }
    public bool IsSelected { get; set; }
  }

  class PreviewStaffViewModel
  {
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Department { get; set; }
    public double? CurrentScore { get; set; }
    public double NewScore { get; set; }
  }
}
