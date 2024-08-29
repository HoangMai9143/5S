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
    private HashSet<StaffViewModel> SelectedStaff = new();
    private double minRange;
    private double maxRange;
    private string searchString = "";

    protected override void OnInitialized()
    {
      staffViewModels = staffList
      .Select(s => new StaffViewModel
      {
        Id = s.Id,
        FullName = s.FullName,
        Department = s.Department,
        CurrentScore = staffScores.TryGetValue(s.Id, out var score) ? score : (double?)null
      })
      .OrderByDescending(s => s.Id)
      .ToList();

      minRange = 0;
      maxRange = maxPossibleScore;
    }

    private IEnumerable<StaffViewModel> FilteredStaffList => staffViewModels;

    private void SelectAllStaff()
    {
      SelectedStaff = new HashSet<StaffViewModel>(staffViewModels);
    }

    private void DeselectAllStaff()
    {
      SelectedStaff.Clear();
    }

    private bool QuickFilter(StaffViewModel staff) =>
    string.IsNullOrWhiteSpace(searchString) ||
    staff.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
    staff.Department.Contains(searchString, StringComparison.OrdinalIgnoreCase);

    void Submit() => MudDialog.Close(DialogResult.Ok(new { MinRange = minRange, MaxRange = maxRange, SelectedStaff = SelectedStaff.Select(s => s.Id).ToList() }));
    void Cancel() => MudDialog.Cancel();
  }


  public class StaffViewModel
  {
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Department { get; set; }
    public double? CurrentScore { get; set; }
  }
}