using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using YouTube.ServerSide.Cacher.Configuration;
using YouTube.ServerSide.Cacher.Models;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace YouTube.ServerSide.Cacher.Services.Protection;

public interface IProtectionService
{
    public bool IsEnabled();
    public bool ValidatePassword(string password);
    public string GenerateApiKey(SupportedSites site, string id);
    public bool ValidateApiKey(string apiKey);
    public string GenerateHashedKey();
    public bool ValidateHashedKey(string hashedKey);
}

public class ProtectionService :IProtectionService
{
    private const string Issuer = "YouTube.ServerSide.Cacher";
    private readonly AppSettings appSettings;
    private readonly byte[] apiKey;
    private readonly byte[] cookieKey;

    public ProtectionService(AppSettings appSettings, ILogger<ProtectionService> logger)
    {
        this.appSettings = appSettings;
        this.apiKey = Encoding.UTF8.GetBytes(appSettings.Protection.ApiSigningKey);
        this.cookieKey = Encoding.UTF8.GetBytes(appSettings.Protection.CookieSigningKey);
        if (appSettings.Protection.Enabled == false)
        {
            logger.LogWarning("Protection is disabled, consider enabling this. Review the readme for more information.");
        }

        if (appSettings.Protection.Password == "changeme" ||
            appSettings.Protection.ApiSigningKey == "change this with environment variables" ||
            appSettings.Protection.CookieSigningKey == "change this with environment variables")
        {
            logger.LogError("Protection secrets are still set to their defaults. Change these. Review the readme for more information.");
        }
    }

    public bool IsEnabled() => appSettings.Protection.Enabled;

    public bool ValidatePassword(string password) => password == appSettings.Protection.Password;

    public string GenerateApiKey(SupportedSites site, string id)
    {
        var key = new SymmetricSecurityKey(apiKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, id),
            new Claim(JwtRegisteredClaimNames.Address, site.ToString())
        };

        var token = new JwtSecurityToken(issuer: Issuer,
            audience: Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateApiKey(string apiKey)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(this.apiKey);

        try
        {
            handler.ValidateToken(apiKey, new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Issuer,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateHashedKey()
    {
        var key = new SymmetricSecurityKey(cookieKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(issuer: Issuer,
            audience: Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateHashedKey(string apiKey)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(cookieKey);

        try
        {
            handler.ValidateToken(apiKey, new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Issuer,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
