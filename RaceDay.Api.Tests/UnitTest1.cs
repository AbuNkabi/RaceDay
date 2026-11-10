using Xunit;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class ProfileDtoTests
{
    [Fact]
    public void ProfileDto_ShouldStoreProfileDetails()
    {
        var profile = new ProfileDto
        {
            UserId = 1,
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            Role = "Participant"
        };

        Assert.Equal(1, profile.UserId);
        Assert.Equal("John", profile.FirstName);
        Assert.Equal("Smith", profile.LastName);
        Assert.Equal("john@example.com", profile.Email);
        Assert.Equal("Participant", profile.Role);
    }
}