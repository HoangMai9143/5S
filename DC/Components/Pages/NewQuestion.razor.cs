using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Components.Dialog;
using DC.Models;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Microsoft.AspNetCore.Components.Web;

namespace DC.Components.Pages
{
  public partial class NewQuestion
  {
    private int activeIndex = 0;

    private List<QuestionModel> questions = new();
    private List<AnswerModel> answers = new();
    private HashSet<AnswerModel> selectedAnswers = new();
    private HashSet<int> existingAnswerIds = new();

    private HashSet<int> originalExistingAnswerIds = new();


    private QuestionModel selectedQuestion;

    protected override async Task OnInitializedAsync()
    {
      await LoadQuestions();
    }
    private async Task LoadQuestions()
    {
      try
      {
        questions = await appDbContext.Set<QuestionModel>()
            .OrderByDescending(q => q.Id)
            .ToListAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading questions: {ex.Message}");
      }
    }

    private async Task HandleTabChanged(int index)
    {
      // if (index == 1 && selectedQuestion == null)
      // {
      //   sb.Add("Please select a question first.", Severity.Warning);

      //   return;
      // }
      if (index == 0 && selectedQuestion != null)
      {
        selectedQuestion = null;
        selectedAnswers.Clear();
      }
      if (selectedQuestion != null)
      {
        await LoadExistingAnswer();
      }
      activeIndex = index;
    }

    private async Task LoadExistingAnswer()
    {
      var existingAnswerIdsList = await appDbContext.Set<QuestionAnswerModel>()
          .Where(sq => sq.QuestionId == selectedQuestion.Id)
          .Select(sq => sq.AnswerId)
          .ToListAsync();

      // Convert to hashset for faster lookup
      existingAnswerIds = new HashSet<int>(existingAnswerIds);
      originalExistingAnswerIds = new HashSet<int>(originalExistingAnswerIds);
      selectedAnswers = new HashSet<AnswerModel>(answers.Where(q => existingAnswerIds.Contains(q.Id)));
      StateHasChanged();
    }
  }
}