using DC.Models;
using DC.Components.Dialog;
using MudBlazor;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Web;


namespace DC.Components.Pages
{
  public partial class Staff
  {

    private List<StaffModel> staffList = new List<StaffModel>();
    private string newStaffFullName = string.Empty;
    private string newStaffDepartment = string.Empty;
    private bool newStaffIsActive = true;
    private string _searchString = string.Empty;
    private System.Timers.Timer _debounceTimer;
    private const int DebounceDelay = 300; // ms
    private bool isLoading = true;

    //* Filter function
    private Func<StaffModel, bool> _quickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_searchString))
        return true;

      if (x.FullName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false)
        return true;

      if (x.Department?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false)
        return true;

      if (x.IsActive.ToString().Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

      if (x.Id.ToString().Contains(_searchString))
        return true;

      return false;
    };

    //* Initialize
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        await LoadStaff();
        _debounceTimer = new System.Timers.Timer(DebounceDelay);
        _debounceTimer.Elapsed += async (sender, e) => await DebounceTimerElapsed();
        _debounceTimer.AutoReset = false;
        isLoading = false;
        StateHasChanged();
      }
    }

    //* Staff CRUD
    private async Task LoadStaff()
    {
      try
      {
        staffList = await appDbContext.Set<StaffModel>()
        .OrderByDescending(s => s.Id)
        .ToListAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading staff: {ex.Message}");
        sb.Add("Error loading staff, please reload page!", Severity.Error);
        staffList = new List<StaffModel>();
        await Task.Delay(1000);
        await LoadStaff();
      }
    }

    private async Task InsertStaff()
    {
      if (!string.IsNullOrWhiteSpace(newStaffFullName) && !string.IsNullOrWhiteSpace(newStaffDepartment))
      {
        // Check for existing staff member with the same full name and department
        var staffExists = await appDbContext.Set<StaffModel>()
            .AnyAsync(s => s.FullName.ToLower() == newStaffFullName.ToLower()
                        && s.Department.ToLower() == newStaffDepartment.ToLower());

        if (staffExists)
        {
          sb.Add("Staff already exist", Severity.Error);
        }
        else
        {
          var newStaff = new StaffModel
          {
            FullName = newStaffFullName,
            Department = newStaffDepartment,
            IsActive = newStaffIsActive
          };

          await appDbContext.Set<StaffModel>().AddAsync(newStaff);
          await appDbContext.SaveChangesAsync();

          await LoadStaff(); // Refresh the staff list

          newStaffFullName = string.Empty;
          newStaffDepartment = string.Empty;
          newStaffIsActive = true;
          sb.Add($"Staff member added successfully with ID: {newStaff.Id}", Severity.Success);
          StateHasChanged();
        }
      }
    }

    private async Task DeleteStaff(StaffModel staffToDelete)
    {
      if (staffToDelete != null)
      {
        int staffToDeleteId = staffToDelete.Id;
        var parameters = new DialogParameters
        {
            { "ContentText", "Are you sure you want to delete this staff member?" },
            { "ButtonText", "Delete" },
            { "Color", Color.Error }
        };

        var options = new DialogOptions()
        {
          MaxWidth = MaxWidth.Small,
          FullWidth = true,
          Position = DialogPosition.Center,
          CloseOnEscapeKey = true,
          FullScreen = false,
        };

        var dialog = await dialogService.ShowAsync<ConfirmDialog>("Delete Staff", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
          await ConfirmedDelete(staffToDelete);
          sb.Add($"Staff member {staffToDeleteId} deleted", Severity.Success);
        }
      }
    }
    private async Task UpdateStaff(StaffModel staffToUpdate)
    {
      var trackedStaff = appDbContext.ChangeTracker.Entries<StaffModel>()
      .FirstOrDefault(e => e.Entity.Id == staffToUpdate.Id)?.Entity;

      if (trackedStaff != null)
      {
        appDbContext.Entry(trackedStaff).State = EntityState.Detached;
      }

      appDbContext.Set<StaffModel>().Update(staffToUpdate);
      await appDbContext.SaveChangesAsync();
      sb.Add($"Staff member {staffToUpdate.Id} updated", Severity.Success);

      await LoadStaff();
      StateHasChanged();
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
        staffList = await appDbContext.Set<StaffModel>()
            .Where(s => s.FullName.ToLower().Contains(searchTerm))
            .OrderByDescending(s => s.Id)
            .ToListAsync();
      }
    }

    //* Dialog function
    private async Task ConfirmedDelete(StaffModel staffToDelete)
    {
      appDbContext.Set<StaffModel>().Remove(staffToDelete);
      await appDbContext.SaveChangesAsync();
      staffList.Remove(staffToDelete);
      StateHasChanged();
    }

    private async Task OpenEditDialog(StaffModel staffToEdit)
    {
      var parameters = new DialogParameters { { "Staff", staffToEdit } };

      var options = new DialogOptions
      {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        Position = DialogPosition.Center,
        CloseOnEscapeKey = true,
        FullScreen = false,
      };

      var dialog = await dialogService.ShowAsync<StaffEditDialog>("Edit Staff", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled && result.Data is StaffModel updatedStaff)
      {
        await UpdateStaff(updatedStaff);
        StateHasChanged();
      }
    }

    //* Event handler
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
      if (e.Key == "Enter")
      {
        await InsertStaff();
      }
      StateHasChanged();
    }
    private async Task OnSearchInput(string value)
    {
      newStaffFullName = value;
      _searchString = value;
      _debounceTimer.Stop();
      _debounceTimer.Start();
    }
    private async Task DebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchStaff(_searchString);
        StateHasChanged();
      });
    }
  }
}