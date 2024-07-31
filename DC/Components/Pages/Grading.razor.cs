using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Components.Dialog;
using DC.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using DC.Data;

namespace DC.Components.Pages
{
	public partial class Grading
	{
		private bool isLoading = true;
		private int activeIndex = 0;
		private List<SurveyModel> surveys = new List<SurveyModel>();
		private SurveyModel selectedSurvey;
		private Dictionary<string, List<StaffModel>> staffByDepartment = new Dictionary<string, List<StaffModel>>();
		private Dictionary<int, double> staffScores = new Dictionary<int, double>();
		private List<StaffModel> allStaff = new List<StaffModel>();
		private string _searchString = "";
		private string _staffSearchString = "";
		private Dictionary<int, string> staffNotes = new Dictionary<int, string>();
		private Dictionary<int, string> tempStaffNotes = new Dictionary<int, string>();
		private Func<SurveyModel, bool> _surveyQuickFilter => x =>
		{
			if (string.IsNullOrWhiteSpace(_searchString))
				return true;

			if (x.Title.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
				return true;

			if (x.Id.ToString().Contains(_searchString))
				return true;

			return false;
		};
		private Func<StaffModel, bool> _staffQuickFilter => x =>
{
	if (string.IsNullOrWhiteSpace(_staffSearchString))
		return true;

	if (x.FullName.Contains(_staffSearchString, StringComparison.OrdinalIgnoreCase))
		return true;

	if (x.Department?.Contains(_staffSearchString, StringComparison.OrdinalIgnoreCase) == true)
		return true;

	return false;
};

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await LoadSurveys();
				await LoadStaff();
				isLoading = false;
				StateHasChanged();
			}
		}

		private async Task LoadSurveys()
		{
			try
			{
				surveys = await appDbContext.SurveyModel
						.OrderByDescending(s => s.Id)
						.ToListAsync();
			}
			catch (Exception ex)
			{
				sb.Add("Error Loading Surveys", Severity.Error);
			}
		}

		private async Task LoadStaff()
		{
			try
			{
				allStaff = await appDbContext.StaffModel.Where(s => s.IsActive).ToListAsync();

				if (selectedSurvey != null)
				{
					// Load scores and notes
					var results = await appDbContext.SurveyResultModel
													.Where(sr => sr.SurveyId == selectedSurvey.Id)
													.ToListAsync();

					staffScores = results.ToDictionary(sr => sr.StaffId, sr => sr.FinalGrade);
					staffNotes = results.ToDictionary(sr => sr.StaffId, sr => sr.Note ?? "");
				}
				else
				{
					staffScores.Clear();
					staffNotes.Clear();
				}
			}
			catch (Exception ex)
			{
				sb.Add("Error Loading Staffs", Severity.Error);
			}
		}

		private string GetStaffNote(int staffId)
		{
			return tempStaffNotes.TryGetValue(staffId, out var note) ? note : staffNotes.TryGetValue(staffId, out var originalNote) ? originalNote : "";
		}

		private void UpdateStaffNote(int staffId, string newNote)
		{
			tempStaffNotes[staffId] = newNote;
		}

		private async Task SubmitStaffNote(int staffId)
		{
			if (selectedSurvey == null)
			{
				sb.Add("Please select a survey first", Severity.Error);
				return;
			}

			if (!tempStaffNotes.TryGetValue(staffId, out var newNote))
			{
				sb.Add("No changes to submit", Severity.Info);
				return;
			}

			try
			{
				var surveyResult = await appDbContext.SurveyResultModel
						.FirstOrDefaultAsync(sr => sr.SurveyId == selectedSurvey.Id && sr.StaffId == staffId);

				if (surveyResult == null)
				{
					surveyResult = new SurveyResultModel
					{
						SurveyId = selectedSurvey.Id,
						StaffId = staffId,
						FinalGrade = 0 // Default value
					};
					appDbContext.SurveyResultModel.Add(surveyResult);
				}

				surveyResult.Note = newNote;
				await appDbContext.SaveChangesAsync();

				staffNotes[staffId] = newNote;
				tempStaffNotes.Remove(staffId);
				sb.Add("Note updated successfully", Severity.Success);
			}
			catch (Exception ex)
			{
				sb.Add($"Error updating note: {ex.Message}", Severity.Error);
			}
		}
		private void HandleTabChanged(int index)
		{
			if (index == 1 && selectedSurvey == null)
			{
				sb.Add("Please select a survey first", Severity.Error);
				return;
			}
			activeIndex = index;
		}

		private void OnSurveySearchInput(string value)
		{
			_searchString = value;
		}

		private async Task OnSelectSurvey(SurveyModel survey)
		{
			selectedSurvey = survey;
			await LoadStaff();
			activeIndex = 1;
			StateHasChanged();
		}

		private async Task OpenStaffGradingDialog(StaffModel staff)
		{
			var parameters = new DialogParameters
			{
				["Staff"] = staff,
				["Survey"] = selectedSurvey
			};

			var options = new DialogOptions { FullScreen = true, CloseButton = true, CloseOnEscapeKey = true };
			var dialog = await dialogService.ShowAsync<GradingDialog>("Grading", parameters, options);
			var result = await dialog.Result;

			if (!result.Canceled)
			{
				var dialogResult = (dynamic)result.Data;
				await SaveGradingResult(dialogResult.GradingResult, dialogResult.Changes);
				await CalculateAndSaveScores(selectedSurvey.Id);
				await LoadStaff();
				StateHasChanged();
			}
		}

		private async Task SaveGradingResult(List<QuestionAnswerModel> gradingResult, bool changes)
		{
			if (!changes)
			{
				sb.Add("No changes were made", Severity.Info);
				return;
			}
			if (changes)
			{
				if (gradingResult != null && gradingResult.Any())
				{
					appDbContext.QuestionAnswerModel.AddRange(gradingResult);
				}

				try
				{
					await appDbContext.SaveChangesAsync();
					sb.Add("Changes saved successfully", Severity.Success);

					await LoadStaff(); // Refresh staffScores after saving
					StateHasChanged(); // Update the UI to reflect the changes
				}
				catch (DbUpdateException ex)
				{
					Console.WriteLine(ex.ToString());

					if (ex.InnerException is SqlException sqlEx)
					{
						switch (sqlEx.Number)
						{
							case 547:
								sb.Add("One or more answers are no longer valid. Please refresh and try again.", Severity.Error);
								break;
							default:
								sb.Add($"Database error occurred: {sqlEx.Message}", Severity.Error);
								break;
						}
					}
					else
					{
						sb.Add("Error saving changes. Please try again.", Severity.Error);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					sb.Add($"An unexpected error occurred: {ex.Message}", Severity.Error);
				}
			}
			else
			{
				sb.Add("No changes were made", Severity.Info);
			}
		}

		private async Task CalculateAndSaveScores(int surveyId)
		{
			try
			{
				var surveyQuestions = await appDbContext.SurveyQuestionModel
																								.Where(sq => sq.SurveyId == surveyId)
																								.Select(sq => new { sq.QuestionId, sq.Question.AnswerType })
																								.ToListAsync();

				var totalPossiblePoints = 0.0;
				foreach (var sq in surveyQuestions)
				{
					var question = await appDbContext.QuestionModel
																					 .Include(q => q.Answers)
																					 .FirstOrDefaultAsync(q => q.Id == sq.QuestionId);

					if (question != null)
					{
						if (sq.AnswerType == AnswerType.SingleChoice && question.Answers.Any())
						{
							totalPossiblePoints += question.Answers.Max(a => a.Points);
						}
						else if (sq.AnswerType == AnswerType.MultipleChoice && question.Answers.Any())
						{
							totalPossiblePoints += question.Answers.Sum(a => a.Points);
						}
					}
				}

				var staffAnswerGroups = await appDbContext.QuestionAnswerModel
																									.Where(qa => qa.SurveyId == surveyId && qa.Answer != null)
																									.Include(qa => qa.Answer)
																									.GroupBy(qa => qa.StaffId)
																									.ToListAsync();

				var scores = new Dictionary<int, double>();

				foreach (var staffGroup in staffAnswerGroups)
				{
					int staffId = staffGroup.Key;
					double totalPoints = 0.0;

					foreach (var sq in surveyQuestions)
					{
						var answers = staffGroup.Where(qa => qa.QuestionId == sq.QuestionId).Select(qa => qa.Answer);

						if (sq.AnswerType == AnswerType.SingleChoice && answers.Any())
						{
							totalPoints += answers.Max(a => a.Points);
						}
						else if (sq.AnswerType == AnswerType.MultipleChoice && answers.Any())
						{
							totalPoints += answers.Sum(a => a.Points);
						}
					}

					double scorePercentage = totalPossiblePoints > 0 ? (totalPoints / totalPossiblePoints) * 100 : 0;
					scores[staffId] = Math.Min(scorePercentage, 100); // Ensure score does not exceed 100

					var surveyResult = await appDbContext.SurveyResultModel
																							 .FirstOrDefaultAsync(r => r.SurveyId == surveyId && r.StaffId == staffId)
																							 ?? new SurveyResultModel
																							 {
																								 SurveyId = surveyId,
																								 StaffId = staffId
																							 };

					surveyResult.FinalGrade = scores[staffId];
					appDbContext.Update(surveyResult);
				}

				await appDbContext.SaveChangesAsync();

				foreach (var kvp in scores)
				{
					var staff = await appDbContext.StaffModel.FindAsync(kvp.Key);
					if (staff != null)
					{
						scores[kvp.Key] = kvp.Value;
					}
				}

				StateHasChanged();
			}
			catch (DbUpdateException ex)
			{
				if (ex.InnerException is SqlException sqlEx)
				{
					HandleSqlException(sqlEx);
				}
				else
				{
					sb.Add("Error saving changes. Please try again.", Severity.Error);
				}
			}
			catch (Exception ex)
			{
				sb.Add($"An error occurred while calculating scores: {ex.Message}", Severity.Error);
			}
		}


		private void HandleSqlException(SqlException sqlEx)
		{
			switch (sqlEx.Number)
			{
				case 547: // Foreign key constraint violation
					sb.Add("One or more answers are no longer valid. Please refresh and try again.", Severity.Error);
					break;
				default:
					sb.Add($"Database error occurred: {sqlEx.Message}", Severity.Error);
					break;
			}
		}
	}
}