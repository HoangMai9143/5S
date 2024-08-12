using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MudBlazor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DC.Components.Pages
{
  public partial class Report
  {
    private bool isLoading = true;
    private int gradedStaff;
    private int totalStaff;
    private double averagePoint;
    private List<ChartSeries> series = new();
    private string[] xAxisLabels = { "0-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-80", "81-90", "91-100" };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        await LoadReportData();
        isLoading = false;
        StateHasChanged();
      }
    }

    private async Task LoadReportData()
    {
      totalStaff = await appDbContext.StaffModel.CountAsync();
      var surveyResults = await appDbContext.SurveyResultModel.ToListAsync();
      gradedStaff = surveyResults.Select(sr => sr.StaffId).Distinct().Count();
      averagePoint = surveyResults.Any() ? surveyResults.Average(sr => sr.FinalGrade) : 0;

      var scoreDistribution = new int[10];
      foreach (var result in surveyResults)
      {
        int index = (int)(result.FinalGrade / 10);
        if (index == 10) index = 9; // Handle 100 score
        scoreDistribution[index]++;
      }

      series = new List<ChartSeries>
        {
            new ChartSeries { Name = "Staff Count", Data = scoreDistribution.Select(x => (double)x).ToArray() }
        };

      isLoading = false;
    }
  }
}