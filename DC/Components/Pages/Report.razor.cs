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
    // Constants
    private const string ALL_DEPARTMENTS = "All departments";

    // Injected Services
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = default!;

    // State Variables
    private bool isLoading = true;
    private bool isExporting = false;
    private string _staffSearchString = "";

    // Data Collections
    private List<SurveyModel> surveys = new();
    private List<string> departments = new();
    private IEnumerable<StaffModel> filteredStaff = new List<StaffModel>();
    private Dictionary<int, double> staffScores = new Dictionary<int, double>();
    private Dictionary<int, string> staffNotes = new Dictionary<int, string>();
    private List<StaffModel> topScoringStaff = new List<StaffModel>();
    private List<StaffModel> lowestScoringStaff = new List<StaffModel>();

    // Chart Data
    private List<ChartSeries> series = new();
    private string[] xAxisLabels = Array.Empty<string>();
    private double[] data = Array.Empty<double>();
    private string[] labels = Array.Empty<string>();
    private List<(int Start, int End, string Label)> scoreRanges = new();

    // Statistics
    private int gradedStaff;
    private int totalStaff;
    private double averageScore;
    private double totalPossiblePoints = 0.0;

    // Selection Properties
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



    //* 1. Initialization and Loading
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        using var scope = ScopeFactory.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        totalStaff = await appDbContext.StaffModel.CountAsync();

        surveys = await appDbContext.SurveyModel.OrderByDescending(s => s.Id).ToListAsync();

        departments = new List<string> { ALL_DEPARTMENTS };
        departments.AddRange(await appDbContext.StaffModel.Select(s => s.Department).Distinct().OrderBy(d => d).ToListAsync());

        _selectedDepartment = ALL_DEPARTMENTS;

        await LoadReportData();
        isLoading = false;
        StateHasChanged();
      }
    }

    //* 2. Data Loading and Processing
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

        if (selectedSurvey != null)
        {
          totalPossiblePoints = await CalculateMaxPoints(selectedSurvey.Id);
          surveyResultsQuery = surveyResultsQuery.Where(sr => sr.SurveyId == selectedSurvey.Id);
        }
        else
        {
          totalPossiblePoints = await CalculateMaxPointsForAllSurveys();
        }

        if (selectedDepartment != ALL_DEPARTMENTS)
        {
          staffQuery = staffQuery.Where(s => s.Department == selectedDepartment);
          surveyResultsQuery = surveyResultsQuery.Where(sr => sr.Staff.Department == selectedDepartment);
        }

        // Materialize the survey results
        var surveyResults = await surveyResultsQuery.ToListAsync();

        // Update averageScore calculation
        averageScore = surveyResults.Any() ? surveyResults.Average(sr => sr.FinalGrade) : 0;

        // Create dynamic score ranges
        scoreRanges.Clear();
        scoreRanges.Add((0, 9, "0"));
        for (int i = 10; i <= (int)totalPossiblePoints; i += 10)
        {
          if (i < totalPossiblePoints)
          {
            scoreRanges.Add((i, Math.Min(i + 9, (int)totalPossiblePoints - 1), $"{i}-{Math.Min(i + 9, (int)totalPossiblePoints - 1)}"));
          }
        }
        scoreRanges.Add(((int)totalPossiblePoints, (int)totalPossiblePoints, $"{totalPossiblePoints}"));

        // Reverse the scoreRanges list
        scoreRanges.Reverse();

        // Update score distribution calculation
        var scoreDistribution = new int[scoreRanges.Count];
        foreach (var result in surveyResults)
        {
          int index = scoreRanges.FindIndex(range => result.FinalGrade >= range.Start && result.FinalGrade <= range.End);
          if (index >= 0)
          {
            scoreDistribution[index]++;
          }
        }

        // Calculate percentages and create chart data
        var totalStaff = scoreDistribution.Sum();
        var dataList = new List<double>();
        var labelList = new List<string>();

        for (int i = scoreRanges.Count - 1; i >= 0; i--)
        {
          if (scoreDistribution[i] > 0)
          {
            double percentage = (double)scoreDistribution[i] / totalStaff * 100;
            var range = scoreRanges[i];

            dataList.Add(scoreDistribution[i]);
            labelList.Add($"{range.Label} ({percentage:F1}%)");
          }
        }

        // Bar chart
        var barChartData = dataList.ToArray().Reverse().ToArray();

        // Set data for bar chart
        series = new List<ChartSeries>
        {
            new ChartSeries { Name = "Staff Count", Data = barChartData }
        };
        xAxisLabels = labelList.Select(l => l.Split(' ')[0]).Reverse().ToArray(); // Reverse to match the data order

        // Pie chart
        data = dataList.ToArray().Reverse().ToArray();
        labels = labelList.ToArray();

        // Set data for bar chart
        series = new List<ChartSeries>
        {
            new ChartSeries { Name = "Staff Count", Data = dataList.ToArray() }
        };
        xAxisLabels = labelList.Select(l => l.Split(' ')[0]).ToArray(); // Use only the range part for x-axis labels

        // Load staff data, which will populate filteredStaff and staffScores
        await LoadStaffData();

        if (selectedSurvey != null && staffScores.Any())
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

    private async Task<double> CalculateMaxPoints(int surveyId)
    {
      using var scope = ScopeFactory.CreateScope();
      var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

      var surveyQuestions = await appDbContext.SurveyQuestionModel
          .Where(sq => sq.SurveyId == surveyId)
          .Include(sq => sq.Question)
          .ThenInclude(q => q.Answers)
          .ToListAsync();

      double maxPoints = 0;

      foreach (var sq in surveyQuestions)
      {
        if (sq.Question.AnswerType == AnswerType.SingleChoice)
        {
          maxPoints += sq.Question.Answers.Max(a => a.Points);
        }
        else if (sq.Question.AnswerType == AnswerType.MultipleChoice)
        {
          maxPoints += sq.Question.Answers.Where(a => a.Points > 0).Sum(a => a.Points);
        }
      }

      return maxPoints;
    }

    private async Task<double> CalculateMaxPointsForAllSurveys()
    {
      using var scope = ScopeFactory.CreateScope();
      var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

      var allSurveyIds = await appDbContext.SurveyModel.Select(s => s.Id).ToListAsync();
      double totalMaxPoints = 0;

      foreach (var surveyId in allSurveyIds)
      {
        totalMaxPoints += await CalculateMaxPoints(surveyId);
      }

      return totalMaxPoints;
    }

    private async Task LoadStaffData()
    {
      using var scope = ScopeFactory.CreateScope();
      var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

      var staffQuery = appDbContext.StaffModel.AsQueryable();
      var surveyResultsQuery = appDbContext.SurveyResultModel.AsQueryable();

      if (selectedSurvey != null)
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
          .Where(sq => selectedSurvey == null || sq.SurveyId == selectedSurvey.Id)
          .ToListAsync();

      // Fetch all answers
      var allAnswers = await appDbContext.QuestionAnswerModel
          .Where(qa => selectedSurvey == null || qa.SurveyId == selectedSurvey.Id)
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

    //* 3. UI Event Handlers
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

    //* 4. Filtering and Searching
    private bool _staffQuickFilter(StaffModel staff)
    {
      if (string.IsNullOrWhiteSpace(_staffSearchString))
        return true;

      if (staff.FullName.Contains(_staffSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (staff.Department.Contains(_staffSearchString, StringComparison.OrdinalIgnoreCase))
        return true;

      // Add score filtering
      if (staffScores.TryGetValue(staff.Id, out var score))
      {
        if (double.TryParse(_staffSearchString, out var searchScore))
        {
          // Check if the score is within ±1 of the searched score
          if (Math.Abs(score - searchScore) <= 1)
            return true;
        }
        else if (score.ToString("F0").Contains(_staffSearchString, StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }

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

    //* 5. Data Export
    private async Task ExportToExcel()
    {
      if (isExporting) return;

      try
      {
        isExporting = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1000); // Add a 1-second delay to prevent spamming

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
        ws.Cell("H2").Value = $"Highest Score: {highestScore:F2}/{totalPossiblePoints:F2}";

        // Insert names of highest scoring staff
        int row = 3;
        foreach (var staff in topScoringStaff)
        {
          ws.Cell(row, 8).Value = staff.FullName;
          row++;
        }

        // Insert lowest score
        var lowestScore = staffScores.Values.Any() ? staffScores.Values.Min() : 0;
        ws.Cell("I2").Value = $"Lowest Score: {lowestScore:F2}/{totalPossiblePoints:F2}";

        // Insert names of lowest scoring staff
        row = 3;
        foreach (var staff in lowestScoringStaff)
        {
          ws.Cell(row, 9).Value = staff.FullName;
          row++;
        }
        // Insert average score
        ws.Cell("G3").Value = $"{averageScore:F2}/{totalPossiblePoints:F2}";

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

          ws.Cell(row, 1).Value = ranking; // Ranking
          ws.Cell(row, 2).Value = staff.Id; // Staff ID
          ws.Cell(row, 3).Value = staff.FullName; // Staff Name
          ws.Cell(row, 4).Value = staff.Department; // Department
          ws.Cell(row, 5).Value = currentScore; // Score as number
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
      finally
      {
        isExporting = false;
        await InvokeAsync(StateHasChanged);
      }
    }

    //* 6. Utility Functions
    private string GetStaffNote(int staffId)
    {
      return staffNotes.TryGetValue(staffId, out var note) ? note : "";
    }

    private void UpdateStaffNote(int staffId, string newNote)
    {
      staffNotes[staffId] = newNote;
    }
  }
}