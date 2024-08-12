using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MudBlazor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DC.Models;
using DC.Components.Dialog;
using Microsoft.AspNetCore.Components;
using DC.Data;

namespace DC.Components.Pages
{
  public partial class Report
  {
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = default!;

    private bool isLoading = true;
    private int gradedStaff;
    private int totalStaff;
    private double averageScore;
    private List<ChartSeries> series = new();
    private string[] xAxisLabels = { "0-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-80", "81-90", "91-100" };
    private double yAxisMax;
    private string _staffSearchString = "";
    private HashSet<StaffModel> selectedItems = new HashSet<StaffModel>();
    private IEnumerable<StaffModel> filteredStaff = new List<StaffModel>();
    private Dictionary<int, double> staffScores = new Dictionary<int, double>();
    private Dictionary<int, string> staffNotes = new Dictionary<int, string>();

    private List<SurveyModel> surveys = new();
    private List<string> departments = new();
    protected SurveyModel? _selectedSurvey;
    private SurveyModel? selectedSurvey
    {
      get => _selectedSurvey;
      set
      {
        if (_selectedSurvey != value)
        {
          _selectedSurvey = value;
          LoadReportData();
        }
      }
    }
    private string? _selectedDepartment;
    private string? selectedDepartment
    {
      get => _selectedDepartment;
      set
      {
        if (_selectedDepartment != value)
        {
          _selectedDepartment = value;
          LoadReportData();
        }
      }
    }

    private const string ALL_DEPARTMENTS = "All departments";
    private const string ALL_SURVEYS = "All surveys";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        using var scope = ScopeFactory.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        surveys = await appDbContext.SurveyModel.OrderByDescending(s => s.Id).ToListAsync();
        surveys.Insert(0, new SurveyModel { Id = 0, Title = ALL_SURVEYS });

        departments = new List<string> { ALL_DEPARTMENTS };
        departments.AddRange(await appDbContext.StaffModel.Select(s => s.Department).Distinct().OrderBy(d => d).ToListAsync());

        _selectedDepartment = ALL_DEPARTMENTS;
        _selectedSurvey = surveys.First(s => s.Title == ALL_SURVEYS);

        await LoadReportData();
        isLoading = false;
        StateHasChanged();
      }
    }

    private async Task LoadStaffData()
    {
      using var scope = ScopeFactory.CreateScope();
      var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

      var staffQuery = appDbContext.StaffModel.AsQueryable();
      var surveyResultsQuery = appDbContext.SurveyResultModel.AsQueryable();

      if (selectedSurvey != null && selectedSurvey.Title != ALL_SURVEYS)
      {
        surveyResultsQuery = surveyResultsQuery.Where(sr => sr.SurveyId == selectedSurvey.Id);
      }

      if (selectedDepartment != ALL_DEPARTMENTS)
      {
        staffQuery = staffQuery.Where(s => s.Department == selectedDepartment);
      }

      filteredStaff = await staffQuery.ToListAsync();

      var surveyResults = await surveyResultsQuery.ToListAsync();
      staffScores = surveyResults.GroupBy(sr => sr.StaffId)
          .ToDictionary(g => g.Key, g => g.Average(sr => sr.FinalGrade));
    }

    private string GetStaffNote(int staffId)
    {
      return staffNotes.TryGetValue(staffId, out var note) ? note : "";
    }

    private void UpdateStaffNote(int staffId, string newNote)
    {
      staffNotes[staffId] = newNote;
    }

    private async Task LoadReportData()
    {
      try
      {
        isLoading = true;
        StateHasChanged();

        using var scope = ScopeFactory.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var staffQuery = appDbContext.StaffModel.AsQueryable();
        var surveyResultsQuery = appDbContext.SurveyResultModel.AsQueryable();

        if (selectedSurvey != null && selectedSurvey.Title != ALL_SURVEYS)
        {
          surveyResultsQuery = surveyResultsQuery.Where(sr => sr.SurveyId == selectedSurvey.Id);
          staffQuery = staffQuery.Where(s => surveyResultsQuery.Any(sr => sr.StaffId == s.Id));
        }

        if (selectedDepartment != ALL_DEPARTMENTS)
        {
          staffQuery = staffQuery.Where(s => s.Department == selectedDepartment);
          surveyResultsQuery = surveyResultsQuery.Where(sr => sr.Staff.Department == selectedDepartment);
        }

        totalStaff = await staffQuery.CountAsync();

        var surveyResults = await surveyResultsQuery.ToListAsync();
        gradedStaff = surveyResults.Select(sr => sr.StaffId).Distinct().Count();
        averageScore = surveyResults.Any() ? surveyResults.Average(sr => sr.FinalGrade) : 0;

        var scoreDistribution = new int[10];
        foreach (var result in surveyResults)
        {
          int index = (int)(result.FinalGrade / 10);
          if (index == 10) index = 9;
          scoreDistribution[index]++;
        }

        yAxisMax = scoreDistribution.Max() * 1.1;
        series = new List<ChartSeries>
        {
            new ChartSeries { Name = "Staff Count", Data = scoreDistribution.Select(x => (double)x).ToArray() }
        };

        staffScores = surveyResults.GroupBy(sr => sr.StaffId)
            .ToDictionary(g => g.Key, g => g.Average(sr => sr.FinalGrade));

        filteredStaff = await staffQuery.ToListAsync();

        isLoading = false;
        StateHasChanged();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading report data: {ex.Message}");
        sb.Add("Error loading report data, please reload page!", Severity.Error);
        await Task.Delay(1000);
        await LoadReportData();
      }
    }

    private async Task OnSurveySelected(SurveyModel value)
    {
      selectedSurvey = value;
      await LoadReportData();
    }

    private async Task OnDepartmentSelected(string value)
    {
      selectedDepartment = value;
      await LoadReportData();
    }

    private async Task OpenStaffGradingDialog(StaffModel staff)
    {
      var parameters = new DialogParameters
      {
        ["Staff"] = staff,
        ["SurveyId"] = selectedSurvey?.Id ?? 0
      };

      var options = new DialogOptions
      {
        CloseButton = true,
        FullScreen = true,
        FullWidth = true,
        CloseOnEscapeKey = true
      };

      var dialog = dialogService.Show<ReportShowDetailDialog>("Staff Report", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled)
      {
        await LoadReportData();
      }
    }

    private bool _staffQuickFilter(StaffModel staff)
    {
      if (string.IsNullOrWhiteSpace(_staffSearchString))
        return true;

      if (staff.FullName.Contains(_staffSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (staff.Department.Contains(_staffSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      return false;
    }
  }
}