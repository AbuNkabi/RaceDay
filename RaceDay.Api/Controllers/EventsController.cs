
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private static readonly string[] AllowedImageTypes =
            { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private static readonly string[] AllowedImageExtensions =
            { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxImageBytes = 5 * 1024 * 1024;

        private readonly RaceDayDbContext _context;
        private readonly IBlobStorageService _blobStorage;

        public EventsController(
            RaceDayDbContext context,
            IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var events = await _context.Events
                .Where(e => e.OrganiserId == userId)
                .ToListAsync();

            return Ok(events);
        }

        // Both roles can view all events
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events.ToListAsync();
            return Ok(events);
        }

        // View one event
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
                return NotFound();

            return Ok(eventItem);
        }

        // Organisers can create events
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEvent(
            [FromBody] Event eventItem)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            eventItem.EventId = 0;
            eventItem.OrganiserId = userId;
            eventItem.ImageUrl = null; // images are only set through the image endpoint

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = eventItem.EventId },
                eventItem);
        }

        // Organisers can update their own events
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(
            int id, [FromBody] Event updatedEvent)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
                return NotFound();

            if (eventItem.OrganiserId != userId)
                return Forbid();

            eventItem.EventName = updatedEvent.EventName;
            eventItem.Description = updatedEvent.Description;
            eventItem.EventDate = updatedEvent.EventDate;
            eventItem.Location = updatedEvent.Location;

            await _context.SaveChangesAsync();

            return Ok(eventItem);
        }

        // Organisers can delete their own events
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
                return NotFound();

            if (eventItem.OrganiserId != userId)
                return Forbid();

            var imageUrl = eventItem.ImageUrl;

            if (await _context.Enrolments.AnyAsync(en => en.EventId == id))
                return Conflict("Cannot delete an event with participant enrolments.");

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            // Best-effort cleanup of the image in Azure Blob Storage
            await _blobStorage.DeleteByUrlAsync(imageUrl);

            return NoContent();
        }

        // ------------------------------------------------------------
        // PART 3: Azure Blob Storage image upload
        // ------------------------------------------------------------
        // API serves images from the PRIVATE Azure container. MVC never connects to Azure.
        [HttpGet("{id:int}/image")]
        [AllowAnonymous]
        public async Task<IActionResult> GetImage(int id, CancellationToken ct)
        {
            var imageName = await _context.Events.AsNoTracking()
                .Where(e => e.EventId == id).Select(e => e.ImageUrl).FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(imageName)) return NotFound();
            try
            {
                var image = await _blobStorage.DownloadByNameAsync(imageName, ct);
                return image == null ? NotFound() : File(image.Value.Bytes, image.Value.ContentType);
            }
            catch (Azure.RequestFailedException) { return StatusCode(502, "Image storage is unavailable."); }
            catch (InvalidOperationException) { return StatusCode(503, "Image storage is not configured."); }
        }

        [HttpPost("{id:int}/image")]
        [Authorize(Roles = "Organiser")]
        [RequestSizeLimit(MaxImageBytes + 1024 * 100)]
        public async Task<IActionResult> UploadImage(
            int id, IFormFile file, CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id, cancellationToken);

            if (eventItem == null)
                return NotFound();

            if (eventItem.OrganiserId != userId)
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest("Please choose an image file.");

            if (file.Length > MaxImageBytes)
                return BadRequest("The image must be 5 MB or smaller.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedImageTypes.Contains(file.ContentType) ||
                !AllowedImageExtensions.Contains(extension))
            {
                return BadRequest("Only JPG, PNG, GIF or WebP images are allowed.");
            }

            string newUrl;
            try
            {
                await using var stream = file.OpenReadStream();
                newUrl = await _blobStorage.UploadImageAsync(
                    stream, file.FileName, file.ContentType, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                // Azure Storage not configured
                return StatusCode(503, ex.Message);
            }
            catch (Azure.RequestFailedException)
            {
                return StatusCode(502, "The image could not be saved to Azure Blob Storage.");
            }

            var oldUrl = eventItem.ImageUrl;
            eventItem.ImageUrl = newUrl;
            await _context.SaveChangesAsync(cancellationToken);

            // Remove the replaced image so storage doesn't fill up with orphans
            await _blobStorage.DeleteByUrlAsync(oldUrl, cancellationToken);

            return Ok(eventItem);
        }

        // ------------------------------------------------------------
        // PART 3: data the MVC front end needs
        // ------------------------------------------------------------

        // Organiser: who is enrolled in one of my events
        [HttpGet("{id:int}/enrolments")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
                return NotFound();

            if (eventItem.OrganiserId != userId)
                return Forbid();

            var enrolments = await (
                from en in _context.Enrolments.AsNoTracking()
                join u in _context.Users.AsNoTracking() on en.UserId equals u.UserId
                where en.EventId == id
                orderby u.FullName
                select new
                {
                    en.EnrolmentId,
                    en.UserId,
                    ParticipantName = u.FullName,
                    u.Email,
                    en.CategoryId,
                    en.Status
                }).ToListAsync();

            return Ok(enrolments);
        }

        // Event-specific categories: organisers can create; everyone can read.
        [HttpGet("{id:int}/categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories(int id)
        {
            if (!await _context.Events.AnyAsync(e => e.EventId == id)) return NotFound();
            var categories = await (from ec in _context.EventCategories.AsNoTracking()
                                    join c in _context.Categories.AsNoTracking()
                                      on ec.CategoryId equals c.CategoryId
                                    where ec.EventId == id
                                    select new { c.CategoryId, c.CategoryName, c.Description,
                                                 c.EntryFee, c.MaximumParticipants }).ToListAsync();
            return Ok(categories);
        }

        [HttpPost("{id:int}/categories")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateCategory(int id, [FromBody] CategoryCreateRequest request)
        {
            var current = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(current, out int userId)) return Unauthorized();
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != userId) return Forbid();
            if (string.IsNullOrWhiteSpace(request.CategoryName) || request.EntryFee < 0 || request.MaximumParticipants <= 0)
                return BadRequest("Provide a category name, nonnegative entry fee and positive capacity.");
            var category = new Category { CategoryName = request.CategoryName.Trim(),
                Description = request.Description?.Trim() ?? "", EntryFee = request.EntryFee,
                MaximumParticipants = request.MaximumParticipants };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            _context.EventCategories.Add(new EventCategory { EventId = id, CategoryId = category.CategoryId });
            await _context.SaveChangesAsync();
            return Created($"/api/events/{id}/categories", category);
        }

        [HttpPut("{id:int}/categories/{categoryId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(int id, int categoryId, [FromBody] CategoryCreateRequest request)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid)) return Unauthorized();
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != uid) return Forbid();
            var assigned = await _context.EventCategories.AnyAsync(x => x.EventId == id && x.CategoryId == categoryId);
            if (!assigned) return NotFound();
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return NotFound();
            if (string.IsNullOrWhiteSpace(request.CategoryName) || request.EntryFee < 0 || request.MaximumParticipants <= 0)
                return BadRequest("Invalid category fields.");
            category.CategoryName = request.CategoryName.Trim();
            category.Description = request.Description?.Trim() ?? "";
            category.EntryFee = request.EntryFee;
            category.MaximumParticipants = request.MaximumParticipants;
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpDelete("{id:int}/categories/{categoryId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id, int categoryId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid)) return Unauthorized();
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != uid) return Forbid();
            var linked = await _context.EventCategories.FirstOrDefaultAsync(x => x.EventId == id && x.CategoryId == categoryId);
            if (linked == null) return NotFound();
            if (await _context.Enrolments.AnyAsync(x => x.EventId == id && x.CategoryId == categoryId))
                return Conflict("Cannot delete a category while participants are enrolled.");
            _context.EventCategories.Remove(linked);
            await _context.SaveChangesAsync();
            // Existing Part 2 categories might be linked to other events.
            if (!await _context.EventCategories.AnyAsync(x => x.CategoryId == categoryId))
            {
                var category = await _context.Categories.FindAsync(categoryId);
                if (category != null) _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }

        [HttpPut("{id:int}/enrolments/{enrolmentId:int}/status")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> ChangeEnrolmentStatus(int id, int enrolmentId, [FromBody] EnrolmentStatusRequest request)
        {
            if (request.Status != "Confirmed" && request.Status != "Rejected" && request.Status != "Pending")
                return BadRequest("Status must be Confirmed, Pending or Rejected.");
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId)) return Unauthorized();
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != userId) return Forbid();
            var enrolment = await _context.Enrolments.FirstOrDefaultAsync(x => x.EventId == id && x.EnrolmentId == enrolmentId);
            if (enrolment == null) return NotFound();
            if (request.Status != "Confirmed" && await _context.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
                return Conflict("Cannot unconfirm an enrolment with a recorded result.");
            enrolment.Status = request.Status;
            await _context.SaveChangesAsync();
            return Ok(enrolment);
        }

        // Any logged-in user: results for one event
        [HttpGet("{id:int}/results")]
        [Authorize]
        public async Task<IActionResult> GetEventResults(int id)
        {
            var exists = await _context.Events.AnyAsync(e => e.EventId == id);

            if (!exists)
                return NotFound();

            var results = await (
                from r in _context.Results.AsNoTracking()
                join en in _context.Enrolments.AsNoTracking() on r.EnrolmentId equals en.EnrolmentId
                join u in _context.Users.AsNoTracking() on en.UserId equals u.UserId
                where en.EventId == id
                orderby r.Position
                select new
                {
                    r.ResultId,
                    r.EnrolmentId,
                    ParticipantName = u.FullName,
                    r.Time,
                    r.Position,
                    en.CategoryId
                }).ToListAsync();

            return Ok(results);
        }
    }
}
