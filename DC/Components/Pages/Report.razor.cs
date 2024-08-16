using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

using ClosedXML.Excel;

using DC.Components.Dialog;
using DC.Data;
using DC.Models;

using MudBlazor;

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
    private IEnumerable<StaffModel> filteredStaff = new List<StaffModel>();
    private Dictionary<int, double> staffScores = new Dictionary<int, double>();
    private Dictionary<int, string> staffNotes = new Dictionary<int, string>();
    private List<StaffModel> topScoringStaff = new List<StaffModel>();
    private List<StaffModel> lowestScoringStaff = new List<StaffModel>();
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

        totalStaff = await appDbContext.StaffModel.CountAsync();

        surveys = await appDbContext.SurveyModel.OrderByDescending(s => s.Id).ToListAsync();
        surveys.Insert(0, new SurveyModel { Id = 0, Title = ALL_SURVEYS });

        departments = new List<string> { ALL_DEPARTMENTS };
        departments.AddRange(await appDbContext.StaffModel.Select(s => s.Department).Distinct().OrderBy(d => d).ToListAsync());

        _selectedDepartment = ALL_DEPARTMENTS;

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

      // Fetch all staff and survey results
      var allStaff = await staffQuery.ToListAsync();
      var surveyResults = await surveyResultsQuery.ToListAsync();

      // Calculate staff scores
      staffScores = surveyResults
          .GroupBy(sr => sr.StaffId)
          .ToDictionary(g => g.Key, g => g.Average(sr => sr.FinalGrade));

      // Fetch all survey questions
      var surveyQuestions = await appDbContext.SurveyQuestionModel
          .Where(sq => selectedSurvey == null || selectedSurvey.Title == ALL_SURVEYS || sq.SurveyId == selectedSurvey.Id)
          .ToListAsync();

      // Fetch all answers
      var allAnswers = await appDbContext.QuestionAnswerModel
          .Where(qa => selectedSurvey == null || selectedSurvey.Title == ALL_SURVEYS || qa.SurveyId == selectedSurvey.Id)
          .ToListAsync();

      // Determine which staff are fully graded
      var fullyGradedStaffIds = allStaff
          .Where(staff => surveyQuestions.All(sq =>
              allAnswers.Any(a => a.StaffId == staff.Id && a.QuestionId == sq.QuestionId)))
          .Select(staff => staff.Id)
          .ToHashSet();

      // Update staffScores to only include fully graded staff
      staffScores = staffScores
          .Where(kvp => fullyGradedStaffIds.Contains(kvp.Key))
          .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

      // Sort staff by score on the client side
      filteredStaff = allStaff
          .OrderByDescending(s => staffScores.TryGetValue(s.Id, out var score) ? score : double.MinValue)
          .ToList();

      gradedStaff = fullyGradedStaffIds.Count;
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

        // totalStaff = await staffQuery.CountAsync();

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

        // Load staff data, which will populate filteredStaff and staffScores
        await LoadStaffData();

        if (selectedSurvey != null && selectedSurvey.Title != ALL_SURVEYS && staffScores.Any())
        {
          // Find the top-scoring staff
          var topScore = staffScores.Values.Max();
          topScoringStaff = filteredStaff
              .Where(s => staffScores.TryGetValue(s.Id, out var score) && Math.Abs(score - topScore) < 0.001)
              .ToList();

          // Find the lowest-scoring staff
          var lowestScore = staffScores.Values.Min();
          lowestScoringStaff = filteredStaff
              .Where(s => staffScores.TryGetValue(s.Id, out var score) && Math.Abs(score - lowestScore) < 0.001)
              .ToList();
        }
        else
        {
          topScoringStaff.Clear();
          lowestScoringStaff.Clear();
        }

        isLoading = false;
        StateHasChanged();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading report data: {ex.Message}");
        sb.Add("Error loading report data, reloading page...", Severity.Error);
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

    private async Task<IEnumerable<SurveyModel>> SearchSurveys(string value, CancellationToken cancellationToken)
    {
      // If string is empty, return all surveys
      if (string.IsNullOrEmpty(value))
        return surveys;

      return surveys.Where(s => s.Title.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IEnumerable<string>> SearchDepartments(string value, CancellationToken cancellationToken)
    {
      // If string is empty, return all departments
      if (string.IsNullOrEmpty(value))
        return departments;

      return departments.Where(d => d.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ExportToExcel()
    {
      try
      {
        var templatePath = "Template/5SReportTemplate.xlsx";

        if (!File.Exists(templatePath))
        {
          sb.Add("Template file not found!", Severity.Error);
          return;
        }

        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);

        // Insert survey title
        ws.Cell("A1").Value = $"{selectedSurvey?.Title ?? "N/A"}";

        // Insert export time
        ws.Cell("G1").Value = $"Export time: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";

        // Insert highest score
        var highestScore = staffScores.Values.Any() ? staffScores.Values.Max() : 0;
        ws.Cell("H2").Value = $"Highest Score: {highestScore:F2}/100";

        // Insert names of highest scoring staff
        int row = 3;
        foreach (var staff in topScoringStaff)
        {
          ws.Cell(row, 8).Value = staff.FullName;
          row++;
        }

        // Insert lowest score
        var lowestScore = staffScores.Values.Any() ? staffScores.Values.Min() : 0;
        ws.Cell("I2").Value = $"Lowest Score: {lowestScore:F2}/100";

        // Insert names of lowest scoring staff
        row = 3;
        foreach (var staff in lowestScoringStaff)
        {
          ws.Cell(row, 9).Value = staff.FullName;
          row++;
        }
        // Insert average score
        ws.Cell("G3").Value = $"{averageScore:F2}/100";

        // Insert number of graded staff
        ws.Cell("G5").Value = $"{gradedStaff}/{totalStaff}";

        // Insert grid data
        row = 3;
        int ranking = 0;
        double previousScore = double.MaxValue;
        foreach (var staff in filteredStaff)
        {
          var currentScore = staffScores.TryGetValue(staff.Id, out var score) ? score : 0;
          if (currentScore < previousScore)
          {
            ranking++;
          }
          previousScore = currentScore;

          ws.Cell(row, 1).Value = ranking;
          ws.Cell(row, 2).Value = staff.Id;
          ws.Cell(row, 3).Value = staff.FullName;
          ws.Cell(row, 4).Value = staff.Department;
          var scoreCell = ws.Cell(row, 5);
          scoreCell.Value = currentScore;
          scoreCell.Style.NumberFormat.Format = "0.00";
          row++;
        }

        // Convert to byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        // Trigger file download
        await js.InvokeVoidAsync("downloadFile", "report.xlsx", Convert.ToBase64String(content));

        sb.Add("Report data exported to Excel!", Severity.Success);
      }
      catch (Exception ex)
      {
        sb.Add($"Error exporting report data to Excel: {ex.Message}", Severity.Error);
      }
    }
  }
}