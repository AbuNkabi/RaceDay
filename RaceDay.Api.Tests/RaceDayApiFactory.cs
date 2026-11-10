using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<RaceDayDbContext>();

            services.AddDbContext<RaceDayDbContext>(options =>
            {
                options.UseInMemoryDatabase("RaceDayTestDatabase");
            });
        });

        builder.UseSetting("Jwt:Key",
            "This-Is-A-Test-Key-That-Is-Long-Enough-For-HS256-123456789");

        builder.UseSetting("Jwt:Issuer", "RaceDay.Api");
        builder.UseSetting("Jwt:Audience", "RaceDay.Api");
    }
}