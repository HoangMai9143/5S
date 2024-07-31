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
  public partial class QuestionEditDialog
  {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

    [Parameter] public int QuestionId { get; set; }

    private QuestionModel currentQuestion = new();
    private List<AnswerModel> currentAnswers = new();

    protected override async Task OnInitializedAsync()
    {
      await LoadQuestion(QuestionId);
    }
    private async Task SaveQuestion()
    {
      try
      {
        if (currentQuestion.Id == 0)
        {
          await appDbContext.Set<QuestionModel>().AddAsync(currentQuestion);
        }
        else
        {
          appDbContext.QuestionModel.Update(currentQuestion);
        }

        await appDbContext.SaveChangesAsync();

        // Remove answers that are no longer in the list
        var existingAnswers = await appDbContext.AnswerModel
        .Where(a => a.QuestionId == currentQuestion.Id)
        .ToListAsync();

        foreach (var existingAnswer in existingAnswers)
        {
          if (!currentAnswers.Exists(a => a.Id == existingAnswer.Id))
          {
            appDbContext.AnswerModel.Remove(existingAnswer);
          }
        }

        // Add or update current answers
        foreach (var answer in currentAnswers)
        {
          answer.QuestionId = currentQuestion.Id;
          if (answer.Id == 0)
          {
            await appDbContext.AnswerModel.AddAsync(answer);
          }
          else
          {
            appDbContext.AnswerModel.Update(answer);
          }
        }

        await appDbContext.SaveChangesAsync();
        sb.Add("Saved successfully.", Severity.Success);

        // Close the dialog
        MudDialog.Close(DialogResult.Ok(true));
      }
      catch (Exception ex)
      {
        sb.Add($"Error saving question: {ex.Message}", Severity.Error);
      }
    }

    private void AddNewAnswer()
    {
      currentAnswers.Add(new AnswerModel
      {
        QuestionId = currentQuestion.Id,
        Points = 1
      });
    }

    private void RemoveAnswer(AnswerModel answer)
    {
      currentAnswers.Remove(answer);
      if (answer.Id != 0)
      {
        appDbContext.AnswerModel.Remove(answer);
      }
    }

    private async Task LoadQuestion(int questionId)
    {
      currentQuestion = await appDbContext.QuestionModel
      .Include(q => q.Answers)
      .FirstOrDefaultAsync(q => q.Id == questionId);

      if (currentQuestion != null)
      {
        currentAnswers = currentQuestion.Answers.ToList();
      }
      else
      {
        currentQuestion = new QuestionModel();
        currentAnswers = new List<AnswerModel>();
      }
    }

    void Submit() => MudDialog.Close(DialogResult.Ok(true));
    void Cancel() => MudDialog.Cancel();
  }
}