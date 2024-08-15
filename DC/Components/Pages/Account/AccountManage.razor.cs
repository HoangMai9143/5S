using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using DC.Models;

using MudBlazor;
using DC.Components.Dialog;

namespace DC.Components.Pages.Account
{
  public partial class AccountManage
  {
    private List<UserAccountModel> userList = new List<UserAccountModel>();
    private string newUsername = string.Empty;
    private string newPassword = string.Empty;
    private string newRole = "User";
    private string _searchString = string.Empty;
    private System.Timers.Timer _debounceTimer;
    private const int DebounceDelay = 300; // ms
    private bool isLoading = true;

    //* Filter function
    private Func<UserAccountModel, bool> _quickFilter => x =>
    {
      if (string.IsNullOrWhiteSpace(_searchString))
        return true;

      if (x.Username?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false)
        return true;

      if (x.Role?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false)
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
        await LoadUsers();
        _debounceTimer = new System.Timers.Timer(DebounceDelay);
        _debounceTimer.Elapsed += async (sender, e) => await DebounceTimerElapsed();
        _debounceTimer.AutoReset = false;
        isLoading = false;
        StateHasChanged();
      }
    }

    //* User CRUD
    private async Task LoadUsers()
    {
      try
      {
        userList = await appDbContext.Set<UserAccountModel>()
        .OrderByDescending(u => u.Id)
        .ToListAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading users: {ex.Message}");
        sb.Add("Error loading users, please reload page!", Severity.Error);
        userList = new List<UserAccountModel>();
        await Task.Delay(1000);
        await LoadUsers();
      }
    }

    private async Task InsertUser()
    {
      if (!string.IsNullOrWhiteSpace(newUsername) && !string.IsNullOrWhiteSpace(newPassword))
      {
        // Check for existing user with the same username
        var userExists = await appDbContext.Set<UserAccountModel>()
            .AnyAsync(u => u.Username.ToLower() == newUsername.ToLower());

        if (userExists)
        {
          sb.Add("User already exists", Severity.Error);
        }
        else
        {
          var newUser = new UserAccountModel
          {
            Username = newUsername,
            Password = newPassword,
            Role = newRole,
            IsActive = true
          };

          await appDbContext.Set<UserAccountModel>().AddAsync(newUser);
          await appDbContext.SaveChangesAsync();

          await LoadUsers(); // Refresh the user list

          newUsername = string.Empty;
          newPassword = string.Empty;
          newRole = "User";
          sb.Add($"User added successfully with ID: {newUser.Id}", Severity.Success);
          StateHasChanged();
        }
      }
    }

    private async Task DeleteUser(UserAccountModel userToDelete)
    {
      if (userToDelete != null)
      {
        int userToDeleteId = userToDelete.Id;
        var parameters = new DialogParameters
        {
            { "ContentText", "Are you sure you want to delete this user?" },
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

        var dialog = await dialogService.ShowAsync<ConfirmDialog>("Delete User", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
          await ConfirmedDelete(userToDelete);
          sb.Add($"User {userToDeleteId} deleted", Severity.Success);
        }
      }
    }

    private async Task UpdateUser(UserAccountModel userToUpdate)
    {
      var trackedUser = appDbContext.ChangeTracker.Entries<UserAccountModel>()
      .FirstOrDefault(e => e.Entity.Id == userToUpdate.Id)?.Entity;

      if (trackedUser != null)
      {
        appDbContext.Entry(trackedUser).State = EntityState.Detached;
      }

      appDbContext.Set<UserAccountModel>().Update(userToUpdate);
      await appDbContext.SaveChangesAsync();
      sb.Add($"User {userToUpdate.Id} updated", Severity.Success);

      await LoadUsers();
      StateHasChanged();
    }

    private async Task SearchUser(string searchTerm)
    {
      if (string.IsNullOrWhiteSpace(searchTerm))
      {
        await LoadUsers();
      }
      else
      {
        searchTerm = searchTerm.ToLower();
        userList = await appDbContext.Set<UserAccountModel>()
            .Where(u => u.Username.ToLower().Contains(searchTerm))
            .OrderByDescending(u => u.Id)
            .ToListAsync();
      }
    }

    //* Dialog function
    private async Task ConfirmedDelete(UserAccountModel userToDelete)
    {
      appDbContext.Set<UserAccountModel>().Remove(userToDelete);
      await appDbContext.SaveChangesAsync();
      userList.Remove(userToDelete);
      StateHasChanged();
    }

    private async Task OpenEditDialog(UserAccountModel userToEdit)
    {
      var parameters = new DialogParameters { { "User", userToEdit } };

      var options = new DialogOptions
      {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        Position = DialogPosition.Center,
        CloseOnEscapeKey = true,
        FullScreen = false,
      };

      var dialog = await dialogService.ShowAsync<AccountEditDialog>("Edit User", parameters, options);
      var result = await dialog.Result;

      if (!result.Canceled && result.Data is UserAccountModel updatedUser)
      {
        await UpdateUser(updatedUser);
        StateHasChanged();
      }
    }

    //* Event handler
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
      if (e.Key == "Enter")
      {
        await InsertUser();
      }
      StateHasChanged();
    }

    private async Task OnSearchInput(string value)
    {
      newUsername = value;
      _searchString = value;
      _debounceTimer.Stop();
      _debounceTimer.Start();
    }

    private async Task DebounceTimerElapsed()
    {
      await InvokeAsync(async () =>
      {
        await SearchUser(_searchString);
        StateHasChanged();
      });
    }
  }
}