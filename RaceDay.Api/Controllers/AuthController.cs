using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        RaceDayDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // PUBLIC REGISTRATION
    // Participant by default. Organiser requires a private registration code.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(
                "First name, last name, email and password are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (request.Password.Length < 8)
        {
            return BadRequest(
                "Password must be at least 8 characters.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(
                "An account with this email already exists.");
        }

        // Organiser registration requires a private server-side code. The default is Participant.
        var roleName = "Participant";
        if (string.Equals(request.Role, "Organiser", StringComparison.OrdinalIgnoreCase))
        {
            var configuredCode = _configuration["Registration:OrganiserCode"];
            if (string.IsNullOrWhiteSpace(configuredCode) ||
                string.IsNullOrWhiteSpace(request.OrganiserCode) ||
                !CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(Encoding.UTF8.GetBytes(configuredCode)),
                    SHA256.HashData(Encoding.UTF8.GetBytes(request.OrganiserCode))))
                return BadRequest("A valid Organiser registration code is required.");
            roleName = "Organiser";
        }
        else if (!string.IsNullOrWhiteSpace(request.Role) &&
                 !request.Role.Equals("Participant", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Invalid role.");

        var role = await _context.Roles
            .SingleOrDefaultAsync(r => r.Name == roleName);

        if (role == null)
        {
            return StatusCode(
                500,
                "The required user role has not been configured.");
        }

        var user = new User
        {
            FullName = $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            Email = email,
            PasswordHash = HashPassword(request.Password),
            RoleId = role.RoleId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Created("/api/auth/register", new
        {
            user.UserId,
            user.FullName,
            user.Email,
            Role = role.Name
        });
    }

    // USER LOGIN
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(
                "Email and password are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user == null ||
            !VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(
                "Invalid email or password.");
        }

        var response = new AuthResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            Role = user.Role.Name,
            Token = GenerateToken(user)
        };

        return Ok(response);
    }

    // PASSWORD HASHING
    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    // PASSWORD VERIFICATION
    private static bool VerifyPassword(
        string password,
        string storedHash)
    {
        var parts = storedHash.Split(':');

        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // JWT TOKEN GENERATION
    private string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                ClaimTypes.Role,
                user.Role.Name)
        };

        var keyText = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is missing.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is missing.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is missing.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyText));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}


