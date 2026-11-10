using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/enrolments"), Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EnrolmentsController(RaceDayDbContext db) { _db = db; }
    private int? CurrentId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet, Authorize(Roles = "Participant")]
    public async Task<IActionResult> Mine()
    {
        var id = CurrentId(); if (id == null) return Unauthorized();
        return Ok(await _db.Enrolments.AsNoTracking().Where(e => e.UserId == id.Value).ToListAsync());
    }

    [HttpGet("{id:int}"), Authorize(Roles = "Participant")]
    public async Task<IActionResult> Get(int id)
    {
        var userId = CurrentId(); if (userId == null) return Unauthorized();
        var e = await _db.Enrolments.AsNoTracking().FirstOrDefaultAsync(e => e.EnrolmentId == id && e.UserId == userId);
        return e == null ? NotFound() : Ok(e);
    }

    public class EnrolBody { public int EventId { get; set; } public int? CategoryId { get; set; } }
    [HttpPost, Authorize(Roles = "Participant")]
    public async Task<IActionResult> Create([FromBody] EnrolBody input)
    {
        var uid = CurrentId(); if (uid == null) return Unauthorized();
        if (!await _db.Events.AnyAsync(e => e.EventId == input.EventId)) return NotFound("Event not found.");
        if (await _db.Enrolments.AnyAsync(e => e.UserId == uid && e.EventId == input.EventId))
            return Conflict("You are already enrolled in this event.");
        var categories = await _db.EventCategories.Where(ec => ec.EventId == input.EventId).Select(ec => ec.CategoryId).ToListAsync();
        if (categories.Count > 0)
        {
            if (input.CategoryId == null || !categories.Contains(input.CategoryId.Value))
                return BadRequest("Choose an available event category.");
            var category = await _db.Categories.FindAsync(input.CategoryId.Value);
            if (category == null) return BadRequest("Invalid category.");
            var used = await _db.Enrolments.CountAsync(e => e.EventId == input.EventId && e.CategoryId == input.CategoryId && e.Status != "Cancelled");
            if (used >= category.MaximumParticipants) return Conflict("This category has reached capacity.");
        }
        else if (input.CategoryId != null) return BadRequest("This event has no matching category.");
        var enrolled = new Enrolment { UserId = uid.Value, EventId = input.EventId, CategoryId = input.CategoryId, Status = "Pending" };
        _db.Enrolments.Add(enrolled); await _db.SaveChangesAsync();
        return Created($"/api/enrolments/{enrolled.EnrolmentId}", enrolled);
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Participant")]
    public async Task<IActionResult> Cancel(int id)
    {
        var uid = CurrentId(); if (uid == null) return Unauthorized();
        var e = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id && e.UserId == uid.Value);
        if (e == null) return NotFound();
        if (await _db.Results.AnyAsync(x => x.EnrolmentId == id)) return Conflict("Cannot cancel an enrolment with a recorded result.");
        _db.Enrolments.Remove(e); await _db.SaveChangesAsync();
        return NoContent();
    }
}
