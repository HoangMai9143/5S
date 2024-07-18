using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DC.Models;
using DC.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DC.Components.Pages.Account
{
  public partial class Register
  {
    [Inject]
    private HttpClient Http { get; set; }

    [Inject]
    private IJSRuntime JS { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [SupplyParameterFromForm]
    public RegisterViewModel Model { get; set; } = new();

    private string? errorMessage;

    private async Task CreateNewAccount()
    {
      errorMessage = null; // Clear any previous error message

      if (Model.Password != Model.ConfirmPassword)
      {
        errorMessage = "Passwords do not match.";
        return;
      }

      try
      {
        var newUser = new User
        {
          Username = Model.Username,
          Password = Model.Password, // Note: In a real application, you should hash the password
          Role = "User" // Default role for new users
        };

        var response = await Http.PostAsJsonAsync("api/User", newUser);

        if (response.IsSuccessStatusCode)
        {
          await JS.InvokeVoidAsync("alert", "Account created successfully!");
          NavigationManager.NavigateTo("/login");
        }
        else
        {
          var error = await response.Content.ReadAsStringAsync();
          errorMessage = $"Failed to create account. Server responded with: {error}";
        }
      }
      catch (Exception ex)
      {
        errorMessage = $"An error occurred while creating the account: {ex.Message}";
      }
    }
  }
}