using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Models;
using DC.Data;
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
    private List<QuestionAnswerModel> existingAnswers = new List<QuestionAnswerModel>();

    protected override async Task OnInitializedAsync()
    {
      existingAnswers = await appDbContext.QuestionAnswerModel
          .Where(qa => qa.StaffId == Staff.Id && qa.SurveyId == Survey.Id)
          .ToListAsync();

      await LoadSurveyQuestions();

      foreach (var question in surveyQuestions)
      {
        var existingAnswer = existingAnswers.FirstOrDefault(a => a.QuestionId == question.QuestionId);
        selectedAnswers[question.QuestionId] = existingAnswer?.AnswerId ?? 0;
      }

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
        selectedAnswers[question.QuestionId] = existingAnswers.FirstOrDefault(a => a.QuestionId == question.QuestionId)?.AnswerId ?? 0;

        selectedMultipleAnswers[question.QuestionId] = new Dictionary<int, bool>();
        foreach (var answer in question.Question.Answers)
        {
          selectedMultipleAnswers[question.QuestionId][answer.Id] =
              existingAnswers.Any(a => a.QuestionId == question.QuestionId && a.AnswerId == answer.Id);
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
      bool changes = false;

      foreach (var sq in surveyQuestions)
      {
        var questionExistingAnswers = existingAnswers.Where(a => a.QuestionId == sq.QuestionId).ToList();

        if (sq.Question.AnswerType == AnswerType.SingleChoice)
        {
          changes |= HandleSingleChoiceQuestion(sq, questionExistingAnswers, gradingResult);
        }
        else if (sq.Question.AnswerType == AnswerType.MultipleChoice)
        {
          changes |= HandleMultipleChoiceQuestion(sq, questionExistingAnswers, gradingResult);
        }
      }

      if (changes)
      {
        await appDbContext.SaveChangesAsync();
      }

      MudDialog.Close(DialogResult.Ok(new { GradingResult = gradingResult, Changes = changes }));
    }

    private bool HandleSingleChoiceQuestion(SurveyQuestionModel sq, List<QuestionAnswerModel> questionExistingAnswers, List<QuestionAnswerModel> gradingResult)
    {
      bool changed = false;

      if (selectedAnswers.TryGetValue(sq.QuestionId, out int answerId) && answerId != 0)
      {
        // Remove all existing answers for this question
        foreach (var existingAnswer in questionExistingAnswers)
        {
          appDbContext.QuestionAnswerModel.Remove(existingAnswer);
        }

        // Add the new answer
        gradingResult.Add(new QuestionAnswerModel
        {
          SurveyId = Survey.Id,
          QuestionId = sq.QuestionId,
          StaffId = Staff.Id,
          AnswerId = answerId
        });

        changed = true;
      }
      else if (questionExistingAnswers.Any())
      {
        // If no answer is selected but there were existing answers, remove them
        foreach (var existingAnswer in questionExistingAnswers)
        {
          appDbContext.QuestionAnswerModel.Remove(existingAnswer);
        }
        changed = true;
      }

      return changed;
    }

    private bool HandleMultipleChoiceQuestion(SurveyQuestionModel sq, List<QuestionAnswerModel> questionExistingAnswers, List<QuestionAnswerModel> gradingResult)
    {
      bool changed = false;

      foreach (var answer in sq.Question.Answers)
      {
        var existingAnswer = questionExistingAnswers.FirstOrDefault(a => a.AnswerId == answer.Id);
        if (selectedMultipleAnswers[sq.QuestionId][answer.Id])
        {
          if (existingAnswer == null)
          {
            gradingResult.Add(new QuestionAnswerModel
            {
              SurveyId = Survey.Id,
              QuestionId = sq.QuestionId,
              StaffId = Staff.Id,
              AnswerId = answer.Id
            });
            changed = true;
          }
        }
        else
        {
          if (existingAnswer != null)
          {
            appDbContext.QuestionAnswerModel.Remove(existingAnswer);
            changed = true;
          }
        }
      }

      return changed;
    }

    private void OnMultipleChoiceChanged(int questionId, int answerId, bool newValue)
    {
      selectedMultipleAnswers[questionId][answerId] = newValue;
      StateHasChanged();
    }
    private void OnSingleChoiceChanged(int questionId, int newValue)
    {
      selectedAnswers[questionId] = newValue;
      StateHasChanged();
    }
  }
}
