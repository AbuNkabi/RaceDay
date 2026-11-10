
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class AuthApiTests : IClassFixture<RaceDayApiFactory>
{
    private readonly HttpClient _client;
    private readonly RaceDayApiFactory _factory;

    public AuthApiTests(RaceDayApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidDetails_ReturnsSuccess()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            db.Database.EnsureDeleted();

            db.Roles.Add(new Role { Name = "Participant" });
            await db.SaveChangesAsync();
        }

        var request = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john.integration@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidDetails_ReturnsToken()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            db.Database.EnsureDeleted();

            var role = new Role { Name = "Participant" };
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            db.Users.Add(new User
            {
                FullName = "John Smith",
                Email = "john.login@example.com",
                PasswordHash = CreatePasswordHash("TestPassword123!"),
                RoleId = role.RoleId
            });

            await db.SaveChangesAsync();
        }

        var request = new LoginRequest
        {
            Email = "john.login@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse =
            await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.Token));
        Assert.Equal("john.login@example.com", authResponse.Email);
        Assert.Equal("Participant", authResponse.Role);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            db.Database.EnsureDeleted();
        }

        var request = new LoginRequest
        {
            Email = "unknown.user@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login", request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            db.Database.EnsureDeleted();

            var role = new Role { Name = "Participant" };
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            db.Users.Add(new User
            {
                FullName = "John Smith",
                Email = "john.login@example.com",
                PasswordHash = CreatePasswordHash("CorrectPassword123!"),
                RoleId = role.RoleId
            });

            await db.SaveChangesAsync();
        }

        var request = new LoginRequest
        {
            Email = "john.login@example.com",
            Password = "WrongPassword123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login", request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static string CreatePasswordHash(string password)
    {
        byte[] salt =
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);

        byte[] hash =
            System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}



