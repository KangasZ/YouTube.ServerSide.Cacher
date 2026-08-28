using Microsoft.AspNetCore.Mvc;
using YouTube.ServerSide.Cacher.Services.Protection;

namespace YouTube.ServerSide.Cacher.Controllers.Api;

[ApiController]
[Route("api/login")]
public class LoginController(IProtectionService protectionService, ILogger<LoginController> logger) : ControllerBase
{
    const string cookieName = "persistantKey";
    [HttpGet]
    public IActionResult Stauts()
    {
        if (Request.Cookies.TryGetValue(cookieName, out var cookieValue)
        && protectionService.ValidateHashedKey(cookieValue))
        {
            return Ok(new { authenticated = true, enabled = protectionService.IsEnabled() });
        }
        return Ok(new { authenticated = false, enabled = protectionService.IsEnabled() });
    }

    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (Request.Cookies.TryGetValue(cookieName, out var cookieValue)
            && !string.IsNullOrWhiteSpace(cookieValue))
        {
            if (!protectionService.ValidateHashedKey(cookieValue))
            {
                Response.Cookies.Delete(cookieName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
                return NoContent();
            }
            else
            {
                var loginResponse = GetLoginResponse();
                Response.Cookies.Append(cookieName, loginResponse.Cookie, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(60)
                });
                return Ok(loginResponse);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (protectionService.ValidatePassword(request.Password))
            {
                var loginResponse = GetLoginResponse();
                Response.Cookies.Append(cookieName, loginResponse.Cookie, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(60)
                });
                return Ok(loginResponse);
            }
        }

        return Unauthorized();
    }

    private LoginResponse GetLoginResponse()
    {
        var cookie = protectionService.GenerateHashedKey();
        var apiKey = protectionService.GenerateApiKey();
        return new LoginResponse()
        {
            Token =  apiKey,
            Cookie = cookie,
        };
    }
}

public record LoginResponse
{
    public required string Token { get; init; }
    public required string Cookie { get; init; }
}

public record LoginRequest
{
    public string Password { get; init; } = string.Empty;
}
