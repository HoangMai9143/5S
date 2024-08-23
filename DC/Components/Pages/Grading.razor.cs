using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using DC.Components.Dialog;
using DC.Models;

using MudBlazor;

namespace DC.Components.Pages
{
	public partial class Grading
	{
		private bool isLoading = true;
		private int activeIndex;
		private List<SurveyModel> surveys = [];
		private List<StaffModel> allStaff = [];
		private SurveyModel? selectedSurvey;
		private string surveySearchString = "";
		private string _staffSearchString = "";
		private System.Timers.Timer? _surveyDebounceTimer;
		private System.Timers.Timer? _staffDebounceTimer;
		private const int DebounceDelay = 300;
		private Dictionary<int, double> staffScores = [];
		private Dictionary<int, string> staffNotes = [];
		private Dictionary<int, double> surveyProgress = new Dictionary<int, double>();
		private readonly Dictionary<int, string> tempStaffNotes = [];
		private double totalPossiblePoints = 0.0;


		private List<StaffModel> filteredStaff => allStaff.Where(FilterStaff).ToList();

		private string staffFilter = "All";

		private bool FilterStaff(StaffModel staff)
		{
			return staffFilter switch
			{
				"Graded" => staffScores.ContainsKey(staff.Id),
				"NotGraded" => !staffScores.ContainsKey(staff.Id),
				_ => true,
			};
		}

		//* Filter function
		private Func<SurveyModel, bool> surveyQuickFilter => x =>
		{
			if (string.IsNullOrWhiteSpace(surveySearchString))
				return true;

			if (x.Title.Contains(surveySearchString, StringComparison.OrdinalIgnoreCase))
				return true;

			if (x.Id.ToString().Contains(surveySearchString))
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

			// Add score filtering
			if (staffScores.TryGetValue(x.Id, out var score))
			{
				if (double.TryParse(_staffSearchString, out var searchScore))
				{
					// Check if the score is within ±1 of the searched score
					if (Math.Abs(score - searchScore) <= 1)
						return true;
				}
				else if (score.ToString("F1").Contains(_staffSearchString, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		};

		//* Initialize
		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await LoadSurveys();
				await LoadStaff();
				await InitializeSurveyProgress();


				_surveyDebounceTimer = new System.Timers.Timer(DebounceDelay);
				_surveyDebounceTimer.Elapsed += async (sender, e) => await SurveyDebounceTimerElapsed();
				_surveyDebounceTimer.AutoReset = false;

				_staffDebounceTimer = new System.Timers.Timer(DebounceDelay);
				_staffDebounceTimer.Elapsed += async (sender, e) => await StaffDebounceTimerElapsed();
				_staffDebounceTimer.AutoReset = false;

				isLoading = false;
				StateHasChanged();
			}
		}

		private async Task InitializeSurveyProgress()
		{
			foreach (var survey in surveys)
			{
				var totalStaff = await appDbContext.StaffModel.CountAsync(s => s.IsActive);
				var gradedStaff = await appDbContext.SurveyResultModel.CountAsync(sr => sr.SurveyId == survey.Id);

				surveyProgress[survey.Id] = totalStaff > 0 ? (double)gradedStaff / totalStaff * 100 : 0;
			}
		}
		private async Task<double> CalculateMaxPoints(int surveyId)
		{
			var surveyQuestions = await appDbContext.SurveyQuestionModel
					.Where(sq => sq.SurveyId == surveyId)
					.Include(sq => sq.Question)
					.ThenInclude(q => q.Answers)
					.ToListAsync();

			double maxPoints = 0;

			foreach (var sq in surveyQuestions)
			{
				if (sq.Question.AnswerType == AnswerType.SingleChoice)
				{
					maxPoints += sq.Question.Answers.Max(a => a.Points);
				}
				else if (sq.Question.AnswerType == AnswerType.MultipleChoice)
				{
					maxPoints += sq.Question.Answers.Where(a => a.Points > 0).Sum(a => a.Points);
				}
			}

			return maxPoints;
		}

		private double GetSurveyProgress(int surveyId)
		{
			return surveyProgress.TryGetValue(surveyId, out var progress) ? progress : 0;
		}

		private string GetSurveyProgressText(int surveyId)
		{
			var progress = GetSurveyProgress(surveyId);
			return $"{progress:F0}% Graded";
		}

		//* Load functions
		private async Task LoadSurveys()
		{
			try
			{
				surveys = await appDbContext.SurveyModel
												.Where(s => s.IsActive)
												.OrderByDescending(s => s.Id)
												.ToListAsync();
			}
			catch (Exception ex)
			{
				sb.Add("Error Loading Surveys", Severity.Error);
				await Task.Delay(1000);
				await LoadSurveys();
			}
		}

		private async Task LoadStaff()
		{
			try
			{
				allStaff = await appDbContext.StaffModel.Where(s => s.IsActive).ToListAsync();

				if (selectedSurvey != null)
				{
					var results = await appDbContext.SurveyResultModel
							.Where(sr => sr.SurveyId == selectedSurvey.Id)
							.ToListAsync();

					staffScores.Clear();
					staffNotes.Clear();

					foreach (var staff in allStaff)
					{
						var result = results.FirstOrDefault(r => r.StaffId == staff.Id);

						if (result != null)
						{
							staffScores[staff.Id] = result.FinalGrade;
							staffNotes[staff.Id] = result.Note ?? "";
						}
						else
						{
							staffNotes[staff.Id] = "";
						}
					}
				}
				else
				{
					staffScores.Clear();
					staffNotes.Clear();
				}
			}
			catch (Exception ex)
			{
				sb.Add("Error Loading Staff", Severity.Error);
				await Task.Delay(1000);
				await LoadStaff();
			}
		}

		//* Event handlers
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
				sb.Add("Please choose a survey first", Severity.Error);
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

		private void OnSurveySearchInput(string value)
		{
			surveySearchString = value;
			_surveyDebounceTimer.Stop();
			_surveyDebounceTimer.Start();
		}
		private async Task SurveyDebounceTimerElapsed()
		{
			await InvokeAsync(async () =>
			{
				await SearchSurveys(surveySearchString);
				StateHasChanged();
			});
		}
		private async Task SearchSurveys(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				await LoadSurveys();
			}
			else
			{
				searchTerm = searchTerm.ToLower();
				var allSurveys = await appDbContext.SurveyModel
								.OrderByDescending(s => s.Id)
								.ToListAsync();

				surveys = allSurveys.Where(s =>
								s.Id.ToString().Contains(searchTerm) ||
								s.Title.ToLower().Contains(searchTerm) ||
								s.StartDate.ToString("dd/MM/yyyy").Contains(searchTerm) ||
								s.EndDate.ToString("dd/MM/yyyy").Contains(searchTerm) ||
								s.CreatedDate.ToString("dd/MM/yyyy HH:mm").Contains(searchTerm) ||
								s.IsActive.ToString().ToLower().Contains(searchTerm)
				).ToList();
			}
		}

		private void OnStaffSearchInput(string value)
		{
			_staffSearchString = value;
			_staffDebounceTimer.Stop();
			_staffDebounceTimer.Start();
		}
		private async Task StaffDebounceTimerElapsed()
		{
			await InvokeAsync(async () =>
			{
				await SearchStaff(_staffSearchString);
				StateHasChanged();
			});
		}
		private async Task SearchStaff(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				await LoadStaff();
			}
			else
			{
				searchTerm = searchTerm.ToLower();
				allStaff = await appDbContext.StaffModel
								.Where(s => s.IsActive &&
																				(s.FullName.ToLower().Contains(searchTerm) ||
																				 s.Department.ToLower().Contains(searchTerm)))
								.ToListAsync();
			}
		}

		private async Task OnSelectSurvey(SurveyModel survey)
		{
			selectedSurvey = survey;
			totalPossiblePoints = await CalculateMaxPoints(survey.Id);
			await LoadStaff();
			activeIndex = 1;
			StateHasChanged();
		}

		private void HandleTabChanged(int index)
		{
			if (index == 1 && selectedSurvey == null)
			{
				sb.Add("Please choose a survey first", Severity.Error);
				return;
			}
			activeIndex = index;
		}

		//* Dialog functions
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

		private async Task OpenAutoGradeDialog()
		{
			var parameters = new DialogParameters
			{
				["staffList"] = allStaff,
				["maxPossibleScore"] = totalPossiblePoints
			};

			var options = new DialogOptions { CloseButton = true, CloseOnEscapeKey = true };
			var dialog = await dialogService.ShowAsync<AutoGradeDialog>("Auto Grading", parameters, options);
			var result = await dialog.Result;

			if (!result.Canceled)
			{
				var dialogResult = (dynamic)result.Data;
				await AutoGradeStaff(dialogResult.MinRange, dialogResult.MaxRange, dialogResult.SelectedStaff);
			}
		}

		private async Task AutoGradeStaff(double minRange, double maxRange, List<int> selectedStaffIds)
		{
			if (selectedSurvey == null)
			{
				sb.Add("Please select a survey first", Severity.Error);
				return;
			}

			var surveyQuestions = await appDbContext.SurveyQuestionModel
					.Where(sq => sq.SurveyId == selectedSurvey.Id)
					.Include(sq => sq.Question)
					.ThenInclude(q => q.Answers)
					.ToListAsync();

			foreach (var staffId in selectedStaffIds)
			{
				var staff = allStaff.FirstOrDefault(s => s.Id == staffId);
				if (staff == null) continue;

				bool scoreInRange;
				int attempts = 0;
				const int maxAttempts = 10;

				do
				{
					// Clear existing answers for this staff
					var existingAnswers = await appDbContext.QuestionAnswerModel
							.Where(qa => qa.SurveyId == selectedSurvey.Id && qa.StaffId == staffId)
							.ToListAsync();
					appDbContext.QuestionAnswerModel.RemoveRange(existingAnswers);

					// Generate new random answers
					var newAnswers = new List<QuestionAnswerModel>();
					foreach (var surveyQuestion in surveyQuestions)
					{
						var randomAnswer = surveyQuestion.Question.Answers.OrderBy(x => Guid.NewGuid()).First();
						newAnswers.Add(new QuestionAnswerModel
						{
							SurveyId = selectedSurvey.Id,
							StaffId = staffId,
							QuestionId = surveyQuestion.QuestionId,
							AnswerId = randomAnswer.Id
						});
					}

					appDbContext.QuestionAnswerModel.AddRange(newAnswers);
					await appDbContext.SaveChangesAsync();

					// Calculate score
					await CalculateAndSaveScores(selectedSurvey.Id);

					scoreInRange = staffScores[staffId] >= minRange && staffScores[staffId] <= maxRange;
					attempts++;

				} while (!scoreInRange && attempts < maxAttempts && staffScores[staffId] < 0);

				if (!scoreInRange)
				{
					sb.Add($"Could not generate a score within the specified range for {staff.FullName} after {maxAttempts} attempts", Severity.Warning);
				}
			}

			sb.Add("Auto grading completed", Severity.Success);
			await LoadStaff();
			StateHasChanged();
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
				totalPossiblePoints = await CalculateMaxPoints(surveyId);

				var staffAnswerGroups = await appDbContext.QuestionAnswerModel
						.Where(qa => qa.SurveyId == surveyId && qa.Answer != null)
						.Include(qa => qa.Answer)
						.GroupBy(qa => qa.StaffId)
						.ToListAsync();

				foreach (var staffGroup in staffAnswerGroups)
				{
					int staffId = staffGroup.Key;
					double totalPoints = 0.0;

					foreach (var answer in staffGroup)
					{
						totalPoints += answer.Answer.Points;
					}

					var surveyResult = await appDbContext.SurveyResultModel
							.FirstOrDefaultAsync(r => r.SurveyId == surveyId && r.StaffId == staffId)
							?? new SurveyResultModel
							{
								SurveyId = surveyId,
								StaffId = staffId
							};

					surveyResult.FinalGrade = totalPoints;
					appDbContext.Update(surveyResult);

					staffScores[staffId] = totalPoints;
				}

				await appDbContext.SaveChangesAsync();
				StateHasChanged();
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