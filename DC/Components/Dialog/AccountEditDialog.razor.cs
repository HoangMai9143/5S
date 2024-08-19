using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace DC.Components.Dialog
{
  public partial class AccountEditDialog
  {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

    [Parameter]
    public UserAccountModel User { get; set; }

    private UserAccountModel currentUser;
    private string newPassword = string.Empty;

    private bool _isPasswordVisible;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    protected override void OnInitialized()
    {
      currentUser = new UserAccountModel
      {
        Id = User.Id,
        Username = User.Username,
        Role = User.Role,
        IsActive = User.IsActive
      };
    }

    private async Task SaveUser()
    {
      try
      {
        var trackedUser = appDbContext.ChangeTracker.Entries<UserAccountModel>()
          .FirstOrDefault(e => e.Entity.Id == currentUser.Id)?.Entity;

        if (trackedUser != null)
        {
          appDbContext.Entry(trackedUser).State = EntityState.Detached;
        }

        var existingUser = await appDbContext.Set<UserAccountModel>().FindAsync(currentUser.Id);
        if (existingUser != null)
        {
          existingUser.Username = currentUser.Username;
          existingUser.Role = currentUser.Role;

          if (!string.IsNullOrWhiteSpace(newPassword))
          {
            // Hash and update the new password only if it's provided
            existingUser.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
          }

          appDbContext.Set<UserAccountModel>().Update(existingUser);
          await appDbContext.SaveChangesAsync();

          // Close the dialog with the updated user as the result
          MudDialog.Close(DialogResult.Ok(existingUser));
        }
        else
        {
          sb.Add("User not found", Severity.Error);
        }
      }
      catch (Exception ex)
      {
        sb.Add($"Error updating user: {ex.Message}", Severity.Error);
      }
    }

    private void Cancel()
    {
      MudDialog.Cancel();
    }

    private void TogglePasswordVisibility()
    {
      if (_isPasswordVisible)
      {
        _isPasswordVisible = false;
        _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
        _passwordInput = InputType.Password;
      }
      else
      {
        _isPasswordVisible = true;
        _passwordInputIcon = Icons.Material.Filled.Visibility;
        _passwordInput = InputType.Text;
      }
    }
  }
}