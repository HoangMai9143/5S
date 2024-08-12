using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MudBlazor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DC.Models;

namespace DC.Components.Pages
{
  public partial class Report
  {
    private bool isLoading = true;
    private int gradedStaff;
    private int totalStaff;
    private double averageScore;
    private List<ChartSeries> series = new();
    private string[] xAxisLabels = { "0-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-80", "81-90", "91-100" };
    private double yAxisMax;

    private List<SurveyModel> surveys = new();
    private List<string> departments = new();
    private SurveyModel? _selectedSurvey;
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

    private const string ALL_DEPARTMENTS = "All";
    private const string ALL_SURVEYS = "All";

    protected override async Task OnInitializedAsync()
    {
      surveys = await appDbContext.SurveyModel.ToListAsync();
      surveys.Insert(0, new SurveyModel { Id = 0, Title = ALL_SURVEYS });

      departments = new List<string> { ALL_DEPARTMENTS };
      departments.AddRange(await appDbContext.StaffModel.Select(s => s.Department).Distinct().OrderBy(d => d).ToListAsync());

      _selectedDepartment = ALL_DEPARTMENTS;
      _selectedSurvey = surveys.First(s => s.Title == ALL_SURVEYS);

      await LoadReportData();
      isLoading = false;
    }

    private async Task LoadReportData()
    {
      isLoading = true;
      StateHasChanged();

      // Query the database
      var staffQuery = appDbContext.StaffModel.AsQueryable();
      var surveyResultsQuery = appDbContext.SurveyResultModel.AsQueryable();

      // Filter the data
      if (selectedSurvey != null && selectedSurvey.Title != ALL_SURVEYS)
      {
        surveyResultsQuery = surveyResultsQuery.Where(sr => sr.SurveyId == selectedSurvey.Id);
      }

      if (selectedDepartment != ALL_DEPARTMENTS)
      {
        staffQuery = staffQuery.Where(s => s.Department == selectedDepartment);
        surveyResultsQuery = surveyResultsQuery.Where(sr => sr.Staff.Department == selectedDepartment);
      }

      // Calculate the report data
      totalStaff = await staffQuery.CountAsync();
      var surveyResults = await surveyResultsQuery.ToListAsync();
      gradedStaff = surveyResults.Select(sr => sr.StaffId).Distinct().Count();
      averageScore = surveyResults.Any() ? surveyResults.Average(sr => sr.FinalGrade) : 0;

      // Calculate the score distribution
      var scoreDistribution = new int[10];
      foreach (var result in surveyResults)
      {
        int index = (int)(result.FinalGrade / 10);
        if (index == 10) index = 9; // handle the case where the grade is 100
        scoreDistribution[index]++;
      }

      yAxisMax = scoreDistribution.Max() * 1.1; // 10% higher than the max value
      series = new List<ChartSeries>
        {
            new ChartSeries { Name = "Staff Count", Data = scoreDistribution.Select(x => (double)x).ToArray() }
        };

      isLoading = false;
      StateHasChanged();
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
  }
}