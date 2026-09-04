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
    public string GenerateWatchKey(SupportedSites site, string id);
    public bool ValidateWatchKey(string apiKey, string requestedVideoId, SupportedSites requestedSite);
    public string GeneratePersistantKey();
    public bool ValidatePersistantKey(string hashedKey);
}

public class ProtectionService :IProtectionService
{
    private const string Issuer = "YouTube.ServerSide.Cacher";
    private readonly AppSettings appSettings;
    private readonly byte[] apiKey;
    private readonly byte[] cookieKey;
    private readonly ILogger<ProtectionService> logger;

    public ProtectionService(AppSettings appSettings, ILogger<ProtectionService> logger)
    {
        this.logger = logger;
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

    public string GenerateWatchKey(SupportedSites site, string id)
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

    public bool ValidateWatchKey(string apiKey, string requestedVideoId, SupportedSites requestedSite)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(this.apiKey);

        try
        {
            var claims = handler.ValidateToken(apiKey, new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Issuer,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var claimVideoId = claims.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name);
            var claimSite = claims.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Address);
            if (claimVideoId is null || claimSite is null)
            {
                logger.LogError("Watch claim Video ID or Site is missing.");
                return false;
            }

            var claimIdParsed = claimVideoId.Value.ToString();
            var siteParseSuccess = Enum.TryParse<SupportedSites>(claimSite.Value.ToString(), out var claimSiteParsed);

            if (siteParseSuccess && claimIdParsed.Equals(requestedVideoId) && claimSiteParsed == requestedSite)
            {
                return true;
            }
            logger.LogError("Watch claim has incorrect video ID or site against requested Resource");
            return false;
        }
        catch
        {
            return false;
        }
    }

    public string GeneratePersistantKey()
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

    public bool ValidatePersistantKey(string apiKey)
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
            logger.LogError("Cookie was not validated");
            return false;
        }
    }
}
