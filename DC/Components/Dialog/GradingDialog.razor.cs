using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class GradingDialog
  {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    [Parameter] public StaffModel Staff { get; set; }
    [Parameter] public SurveyModel Survey { get; set; }

    private bool isLoading = true;
    private List<SurveyQuestionModel> surveyQuestions = new List<SurveyQuestionModel>();
    private Dictionary<int, int> selectedAnswers = new Dictionary<int, int>();
    private Dictionary<int, Dictionary<int, bool>> selectedMultipleAnswers = new Dictionary<int, Dictionary<int, bool>>();

    protected override async Task OnInitializedAsync()
    {
      await LoadSurveyQuestions();
      isLoading = false;
    }

    private async Task LoadSurveyQuestions()
    {
      surveyQuestions = await appDbContext.SurveyQuestionModel
        .Include(sq => sq.Question)
        .ThenInclude(q => q.Answers)
        .Where(sq => sq.SurveyId == Survey.Id)
        .ToListAsync();

      foreach (var question in surveyQuestions)
      {
        selectedAnswers[question.QuestionId] = 0;
        selectedMultipleAnswers[question.QuestionId] = new Dictionary<int, bool>();
        foreach (var answer in question.Question.Answers)
        {
          selectedMultipleAnswers[question.QuestionId][answer.Id] = false;
        }
      }
    }

    private void Cancel()
    {
      MudDialog.Cancel();
    }

    private async Task Submit()
    {
      var gradingResult = new List<QuestionAnswerModel>();

      foreach (var sq in surveyQuestions)
      {
        if (sq.Question.AnswerType == AnswerType.SingleChoice)
        {
          if (selectedAnswers.TryGetValue(sq.QuestionId, out int answerId) && answerId != 0)
          {
            var answerExists = await appDbContext.AnswerModel.AnyAsync(a => a.Id == answerId);
            if (!answerExists)
            {
              MudDialog.Close(DialogResult.Cancel());
              return;
            }

            gradingResult.Add(new QuestionAnswerModel
            {
              SurveyId = Survey.Id,
              QuestionId = sq.QuestionId,
              StaffId = Staff.Id,
              AnswerId = answerId
            });
          }
        }
        else if (sq.Question.AnswerType == AnswerType.MultipleChoice)
        {
          foreach (var answer in sq.Question.Answers)
          {
            if (selectedMultipleAnswers[sq.QuestionId][answer.Id])
            {
              var answerExists = await appDbContext.AnswerModel.AnyAsync(a => a.Id == answer.Id);
              if (!answerExists)
              {
                MudDialog.Close(DialogResult.Cancel());
                return;
              }

              gradingResult.Add(new QuestionAnswerModel
              {
                SurveyId = Survey.Id,
                QuestionId = sq.QuestionId,
                StaffId = Staff.Id,
                AnswerId = answer.Id
              });
            }
          }
        }
      }

      MudDialog.Close(DialogResult.Ok(gradingResult));
    }

    private void OnMultipleChoiceChanged(int questionId, int answerId, bool newValue)
    {
      selectedMultipleAnswers[questionId][answerId] = newValue;
      StateHasChanged();
    }
  }
}