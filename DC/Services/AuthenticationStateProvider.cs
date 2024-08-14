using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace DC.Services
{
  public class CustomAuthenticationStateProvider : AuthenticationStateProvider
  {
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, ILogger<CustomAuthenticationStateProvider> logger)
    {
      _jsRuntime = jsRuntime;
      _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
      var emptyState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

      try
      {
        var userInfoJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userInfo");

        _logger.LogInformation($"Retrieved userInfo from localStorage: {userInfoJson}");

        if (string.IsNullOrEmpty(userInfoJson))
        {
          _logger.LogInformation("No userInfo found in localStorage");
          return emptyState;
        }

        var userInfo = JsonSerializer.Deserialize<UserInfo>(userInfoJson);

        var claims = new List<Claim>
        {
          new Claim(ClaimTypes.Name, userInfo.Username),
          new Claim(ClaimTypes.Role, userInfo.Role)
        };

        var identity = new ClaimsIdentity(claims, "LocalStorageAuth");
        var user = new ClaimsPrincipal(identity);

        _logger.LogInformation($"Authenticated user: {userInfo.Username} with role: {userInfo.Role}");

        return new AuthenticationState(user);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving authentication state");
        return emptyState;
      }
    }

    public async Task UpdateAuthenticationState()
    {
      _logger.LogInformation("Updating authentication state");
      var authState = await GetAuthenticationStateAsync();
      NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }
  }

  public class UserInfo
  {
    public string Username { get; set; }
    public string Role { get; set; }
  }
}