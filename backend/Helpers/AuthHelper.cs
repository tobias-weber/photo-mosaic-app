using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend.Helpers;

public static class AuthHelper
{
    public record AuthResult(string Token, DateTime Expiration);

    public static async Task<AuthResult> GenerateTokenAsync(User user, IConfiguration config,
        UserManager<User> userManager)
    {
        var userRoles = await userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var userRole in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            expires: DateTime.UtcNow.AddHours(double.Parse(config["Jwt:ExpiresInHours"]!)),
            claims: authClaims,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new AuthResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            token.ValidTo
        );
    }

    public static async Task<RefreshToken> AddRefreshTokenAsync(
        User user,
        HttpContext httpContext,
        IConfiguration config,
        AppDbContext db)
    {
        var refreshToken = AuthHelper.GenerateRefreshToken(user, httpContext, config);

        // remove expired refresh tokens
        var now = DateTime.UtcNow;
        var expiredTokens = await db.RefreshTokens.Where(rt => rt.Expires < now).ToListAsync();
        db.RefreshTokens.RemoveRange(expiredTokens);

        // add new token
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();
        return refreshToken;
    }

    public static RefreshToken GenerateRefreshToken(User user, HttpContext httpContext,
        IConfiguration config)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), // secure random
            UserId = user.Id,
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(double.Parse(config["Jwt:RefreshExpiresInDays"]!)), // adjust duration
            IsRevoked = false
        };
        return refreshToken;
    }


    public static async Task<RefreshToken?> GetValidRefreshTokenAsync(
        HttpContext httpContext,
        AppDbContext db)
    {
        var token = httpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var existingToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        if (existingToken == null ||
            existingToken.IsRevoked ||
            existingToken.Expires < DateTime.UtcNow ||
            existingToken.UserAgent != userAgent)
        {
            return null;
        }

        return existingToken;
    }

    public static void SetRefreshCookie(HttpContext httpContext, RefreshToken refreshToken)
    {
        httpContext.Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.Expires
        });
    }

    public static async Task RevokeRefreshTokenAndCookieAsync(HttpContext httpContext, AppDbContext db)
    {
        var token = httpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token))
            return;

        var stored = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
        if (stored is not null)
        {
            stored.IsRevoked = true;
            await db.SaveChangesAsync();
        }

        // Delete cookie on client
        httpContext.Response.Cookies.Delete("refreshToken");
    }
}