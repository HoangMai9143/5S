using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DC.Components.Pages
{
  public partial class Question
  {

  }
}
public class QuestionModel
{
  public string Title { get; set; }
  public string[] Options { get; set; } = { "Bad", "Okay", "Good", "Very Good" };
}