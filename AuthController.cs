using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureWebApi.Data;
using SecureWebApi.DTOs;
using SecureWebApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SecureWebApi.Controllers;
/// <summary>
/// Provides user registration, login and token refresh operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // =========================
    // REGISTER
    // =========================
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">Registration details.</param>
    /// <returns>Registration result.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Username == request.Username);

        if (existingUser != null)
        {
            return BadRequest(new
            {
                message = "Username already exists."
            });
        }

        var user = new User
        {
            Username = request.Username,
            Role = "User"
        };

        var passwordHasher = new PasswordHasher<User>();

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                request.Password);

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User registered successfully."
        });
    }


    // =========================
    // LOGIN
    // =========================
    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>JWT access token and refresh token.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Username == request.Username);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        var passwordHasher =
            new PasswordHasher<User>();

        var result =
            passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        // Generate Access Token
        var accessToken =
            GenerateAccessToken(user);

        // Generate Refresh Token
        var refreshToken =
            GenerateRefreshToken();

        // Hash Refresh Token
        var refreshTokenHash =
            HashToken(refreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAtUtc =
                DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(
            refreshTokenEntity);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken
        });
    }


    // =========================
    // GENERATE ACCESS TOKEN
    // =========================

    private string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Username),

            new Claim(
                ClaimTypes.Role,
                user.Role)
        };

        var jwtKey =
            _configuration["Jwt:Key"];

        var jwtIssuer =
            _configuration["Jwt:Issuer"];

        var jwtAudience =
            _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT Key is missing.");
        }

        if (string.IsNullOrEmpty(jwtIssuer))
        {
            throw new InvalidOperationException(
                "JWT Issuer is missing.");
        }

        if (string.IsNullOrEmpty(jwtAudience))
        {
            throw new InvalidOperationException(
                "JWT Audience is missing.");
        }

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires:
                DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }


    // =========================
    // GENERATE REFRESH TOKEN
    // =========================

    private string GenerateRefreshToken()
    {
        var randomBytes =
            RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(
            randomBytes);
    }


    // =========================
    // REFRESH TOKEN
    // =========================
    /// <summary>
    /// Rotates the refresh token and generates a new access token.
    /// </summary>
    /// <param name="request">Current refresh token.</param>
    /// <returns>New access and refresh tokens.</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request)
    {
        var tokenHash =
            HashToken(request.RefreshToken);

        var storedToken =
            await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.TokenHash == tokenHash);

        if (storedToken == null)
        {
            return Unauthorized(new
            {
                message = "Invalid refresh token."
            });
        }

        if (storedToken.RevokedAtUtc != null)
        {
            return Unauthorized(new
            {
                message =
                    "Refresh token has been revoked."
            });
        }

        if (storedToken.ExpiresAtUtc <=
            DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                message =
                    "Refresh token has expired."
            });
        }

        // Generate new Access Token
        var newAccessToken =
            GenerateAccessToken(
                storedToken.User);

        // Generate new Refresh Token
        var newRefreshToken =
            GenerateRefreshToken();

        var newRefreshTokenHash =
            HashToken(newRefreshToken);

        // Revoke old token
        storedToken.RevokedAtUtc =
            DateTime.UtcNow;

        storedToken.ReplacedByTokenHash =
            newRefreshTokenHash;

        // Create new refresh token
        var newRefreshTokenEntity =
            new RefreshToken
            {
                UserId =
                    storedToken.UserId,

                TokenHash =
                    newRefreshTokenHash,

                ExpiresAtUtc =
                    DateTime.UtcNow.AddDays(7)
            };

        _context.RefreshTokens.Add(
            newRefreshTokenEntity);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }


    // =========================
    // HASH REFRESH TOKEN
    // =========================

    private string HashToken(string token)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

        return Convert.ToBase64String(bytes);
    }

}
