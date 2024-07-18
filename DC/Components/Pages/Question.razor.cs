using System.Collections.Generic;
using System.Threading.Tasks;
using MudBlazor;
using DC.Models;
using Microsoft.EntityFrameworkCore;

namespace DC.Components.Pages
{
  public partial class Question
  {
    private List<QuestionModel> questions = new();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
      try
      {
        questions = await appDbContext.Set<QuestionModel>().ToListAsync();
      }
      finally
      {
        isLoading = false;
        StateHasChanged();
      }
    }
  }
}