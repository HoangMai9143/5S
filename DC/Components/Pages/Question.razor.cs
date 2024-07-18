using System.Collections.Generic;
using System.Threading.Tasks;
using MudBlazor;
using DC.Models;
using Microsoft.EntityFrameworkCore;


namespace DC.Components.Pages
{
  public partial class Question
  {
    private List<QuestionModel> questions = [];
    private HashSet<QuestionModel> selectedQuestions = new();
    private string _selectedItemText = "No row clicked";
    private bool _selectOnRowClick = false;
    private bool _selectionChangeable = true;
    private MudTable<QuestionModel> _table;

    protected override async Task OnInitializedAsync()
    {
      await LoadQuestions();
    }

    private async Task LoadQuestions()
    {
      questions = await appDbContext.Set<QuestionModel>().ToListAsync();
    }

    private void OnRowClick(TableRowClickEventArgs<QuestionModel> args)
    {
      _selectedItemText = $"{args.Item.QuestionContext} (ID: {args.Item.Id})";
    }
  }
}