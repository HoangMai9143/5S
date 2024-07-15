namespace DC.Models
{
  public class QuestionModel
  {
    public string Title { get; set; }
    public string[] Options { get; set; } = { "Bad", "Okay", "Good", "Very Good" };
  }
}