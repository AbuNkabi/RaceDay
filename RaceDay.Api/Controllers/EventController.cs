
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
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

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

