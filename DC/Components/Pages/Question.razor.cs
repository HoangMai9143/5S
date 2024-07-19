using System.Collections.Generic;
using System.Threading.Tasks;
using MudBlazor;
using DC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Web;

namespace DC.Components.Pages
{
  public partial class Question
  {
    private List<QuestionModel> questions = new List<QuestionModel>();
    private string newQuestionText;

    protected override async Task OnInitializedAsync()
    {
      try
      {
        questions = await appDbContext.Set<QuestionModel>().ToListAsync();
      }
      catch (Exception ex)
      {
        // Log the exception
        Console.WriteLine($"Error loading questions: {ex.Message}");
        questions = new List<QuestionModel>();
      }
    }

    private async Task InsertQuestion()
    {
      if (!string.IsNullOrWhiteSpace(newQuestionText))
      {
        var newQuestion = new QuestionModel { QuestionContext = newQuestionText };
        await appDbContext.Set<QuestionModel>().AddAsync(newQuestion);
        await appDbContext.SaveChangesAsync();
        questions.Add(newQuestion);
        newQuestionText = string.Empty;
        StateHasChanged();
      }
    }
    private async Task DeleteQuestion(QuestionModel questionToDelete)
    {
      if (questionToDelete != null)
      {
        appDbContext.Set<QuestionModel>().Remove(questionToDelete);
        await appDbContext.SaveChangesAsync();
        questions.Remove(questionToDelete);
        StateHasChanged();
      }
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
      if (e.Key == "Enter")
      {
        await InsertQuestion();
      }
    }
  }
}