using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    public interface IProfileController
    {
        Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request);
    }
}