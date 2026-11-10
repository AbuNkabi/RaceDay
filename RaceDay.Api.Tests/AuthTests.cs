using Xunit;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class AuthTests
{
    [Fact]
    public void RegisterRequest_ShouldStoreRegistrationDetails()
    {
        var request = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            Password = "TestPassword123!"
        };

        Assert.Equal("John", request.FirstName);
        Assert.Equal("Smith", request.LastName);
        Assert.Equal("john@example.com", request.Email);
        Assert.Equal("TestPassword123!", request.Password);
    }

    [Fact]
    public void LoginRequest_ShouldStoreLoginDetails()
    {
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "TestPassword123!"
        };

        Assert.Equal("john@example.com", request.Email);
        Assert.Equal("TestPassword123!", request.Password);
    }

    [Fact]
    public void AuthResponse_ShouldStoreUserAndTokenDetails()
    {
        var response = new AuthResponse
        {
            UserId = 1,
            Email = "john@example.com",
            Role = "Participant",
            Token = "sample-token"
        };

        Assert.Equal(1, response.UserId);
        Assert.Equal("john@example.com", response.Email);
        Assert.Equal("Participant", response.Role);
        Assert.Equal("sample-token", response.Token);
    }
}