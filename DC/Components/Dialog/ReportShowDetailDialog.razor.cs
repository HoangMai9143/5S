using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Microsoft.EntityFrameworkCore;

namespace DC.Components.Dialog
{
  public partial class ReportShowDetailDialog
  {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    [Parameter] public StaffModel Staff { get; set; }
    [Parameter] public int SurveyId { get; set; }

    private double? score;
    private string surveyNote;
    private List<SurveyQuestionModel> surveyQuestions = new List<SurveyQuestionModel>();
    private List<QuestionAnswerModel> questionAnswers = new List<QuestionAnswerModel>();

    protected override async Task OnInitializedAsync()
    {
      await LoadStaffData();
    }

    private async Task LoadStaffData()
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
    }

    private void Cancel()
    {
      MudDialog.Cancel();
    }

    private async Task Submit()
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

      await appDbContext.SaveChangesAsync();

      MudDialog.Close(DialogResult.Ok(true));
    }
  }
}