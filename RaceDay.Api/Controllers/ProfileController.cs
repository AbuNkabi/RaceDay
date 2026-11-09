
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.Api.Data;
using RaceDay.Contracts;
using Microsoft.AspNetCore.Identity;

namespace RaceDay.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase, IProfileController
    {
        private readonly RaceDayDbContext _context;

        public ProfileController(RaceDayDbContext context)
        {
            _context = context;
        }


        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateProfileRequest request)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Full name and email are required.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound();

            var emailExists = await _context.Users.AnyAsync(
                u => u.Email == request.Email && u.UserId != userId);

            if (emailExists)
                return Conflict("That email address is already in use.");

            user.FullName = request.FullName.Trim();
            user.Email = request.Email.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Profile updated successfully.",
                user.UserId,
                user.FullName,
                user.Email
            });
        }
    }
}
