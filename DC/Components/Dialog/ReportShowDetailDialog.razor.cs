using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

using DC.Models;

using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class ReportShowDetailDialog
  {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    [Parameter] public StaffModel Staff { get; set; }
    [Parameter] public int SurveyId { get; set; }

    private double? score;
    private string surveyNote;
    private Dictionary<int, string?> questionNotes = [];
    private List<SurveyQuestionModel> surveyQuestions = [];
    private List<QuestionAnswerModel> questionAnswers = [];

    private string questionNote;

    protected override async Task OnInitializedAsync()
    {
      await LoadReportData();
    }

    private async Task LoadReportData()
    {
      var surveyResult = await appDbContext.SurveyResultModel
        .FirstOrDefaultAsync(sr => sr.StaffId == Staff.Id && sr.SurveyId == SurveyId);

      if (surveyResult != null)
      {
        score = surveyResult.FinalGrade;
        surveyNote = surveyResult.Note;
      }

      surveyQuestions = await appDbContext.SurveyQuestionModel
        .Include(sq => sq.Question)
        .ThenInclude(q => q.Answers)
        .Where(sq => sq.SurveyId == SurveyId)
        .ToListAsync();

      questionAnswers = await appDbContext.QuestionAnswerModel
        .Where(qa => qa.StaffId == Staff.Id && qa.SurveyId == SurveyId)
        .ToListAsync();

      // Fetch question notes from ResultModel
      var results = await appDbContext.ResultModel
        .Where(r => r.StaffId == Staff.Id && r.SurveyId == SurveyId)
        .ToListAsync();

      questionNotes = results.ToDictionary(r => r.QuestionId, r => r.Note);
    }

    private void Cancel()
    {
      MudDialog.Cancel();
    }

    private async Task Submit()
    {
      try
      {
        var surveyResult = await appDbContext.SurveyResultModel
          .FirstOrDefaultAsync(sr => sr.StaffId == Staff.Id && sr.SurveyId == SurveyId);

        if (surveyResult == null)
        {
          surveyResult = new SurveyResultModel
          {
            StaffId = Staff.Id,
            SurveyId = SurveyId
          };
          appDbContext.SurveyResultModel.Add(surveyResult);
        }

        surveyResult.FinalGrade = score ?? 0;
        surveyResult.Note = surveyNote;

        foreach (var note in questionNotes)
        {
          var result = await appDbContext.ResultModel
            .FirstOrDefaultAsync(r => r.StaffId == Staff.Id && r.SurveyId == SurveyId && r.QuestionId == note.Key);

          if (result != null)
          {
            result.Note = note.Value;
          }
          else
          {
            result = new ResultModel
            {
              StaffId = Staff.Id,
              SurveyId = SurveyId,
              QuestionId = note.Key,
              Note = note.Value
            };
            appDbContext.ResultModel.Add(result);
          }
        }

        await appDbContext.SaveChangesAsync();
        sb.Add("Saved", Severity.Success);

        MudDialog.Close(DialogResult.Ok(true));
      }
      catch (Exception ex)
      {
        sb.Add(ex.Message, Severity.Error);
      }
    }

    private Color GetChipColor(int points)
    {
      if (points > 0)
        return Color.Success;
      else if (points < 0)
        return Color.Error;
      else
        return Color.Default;
    }
    private string GetChipText(int points)
    {
      if (points > 0)
        return $"+{points}";
      else
        return points.ToString();
    }
  }
}