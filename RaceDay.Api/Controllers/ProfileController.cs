
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.Api.Data;
using RaceDay.Contracts;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IBlobStorageService _blobStorage;

        public ProfileController(RaceDayDbContext context, IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        // PART 3: lets the MVC app show the logged-in user's profile
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.ProfileImageUrl,
                Role = user.Role.Name
            });
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

            // Login lower-cases emails, so store them lower-cased here too
            var email = request.Email.Trim().ToLowerInvariant();

            var emailExists = await _context.Users.AnyAsync(
                u => u.Email == email && u.UserId != userId);

            if (emailExists)
                return Conflict("That email address is already in use.");

            user.FullName = request.FullName.Trim();
            user.Email = email;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Profile updated successfully.",
                user.UserId,
                user.FullName,
                user.Email
            });
        }
        [HttpPost("me/image")]
        [RequestSizeLimit(5 * 1024 * 1024 + 100 * 1024)]
        public async Task<IActionResult> UploadProfileImage(IFormFile file, CancellationToken ct)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId)) return Unauthorized();
            if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024) return BadRequest("Upload an image up to 5 MB.");
            if (!new[] { "image/png", "image/jpeg", "image/webp" }.Contains(file.ContentType) ||
                !new[] { ".png", ".jpg", ".jpeg", ".webp" }.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
                return BadRequest("Supported image formats: JPG, PNG, WEBP.");
            var user = await _context.Users.FindAsync(new object[] { userId }, ct);
            if (user == null) return NotFound();
            string uploaded;
            try
            {
                await using var stream = file.OpenReadStream();
                uploaded = await _blobStorage.UploadImageAsync(stream, file.FileName, file.ContentType, ct);
            }
            catch (InvalidOperationException) { return StatusCode(503, "Storage is not configured."); }
            catch (Azure.RequestFailedException) { return StatusCode(502, "Azure image upload failed."); }
            var old = user.ProfileImageUrl;
            user.ProfileImageUrl = uploaded;
            await _context.SaveChangesAsync(ct);
            await _blobStorage.DeleteByUrlAsync(old, ct);
            return Ok(new { user.ProfileImageUrl });
        }

        [HttpGet("me/image")]
        public async Task<IActionResult> ProfileImage(CancellationToken ct)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId)) return Unauthorized();
            var fileName = await _context.Users.AsNoTracking().Where(x => x.UserId == userId)
                .Select(x => x.ProfileImageUrl).FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(fileName)) return NotFound();
            try
            {
                var file = await _blobStorage.DownloadByNameAsync(fileName, ct);
                return file == null ? NotFound() : File(file.Value.Bytes, file.Value.ContentType);
            }
            catch (InvalidOperationException) { return StatusCode(503, "Storage is not configured."); }
            catch (Azure.RequestFailedException) { return StatusCode(502, "Could not retrieve image."); }
        }
    }
}
