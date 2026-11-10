using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/results"), Authorize]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) { _db = db; }
    private int? UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> GetResults()
    {
        var uid = UserId(); if (uid == null) return Unauthorized();
        bool isOrganiser = User.IsInRole("Organiser");
        bool isParticipant = User.IsInRole("Participant");
        var query = from result in _db.Results.AsNoTracking()
                    join en in _db.Enrolments on result.EnrolmentId equals en.EnrolmentId
                    join ev in _db.Events on en.EventId equals ev.EventId
                    where (isOrganiser && ev.OrganiserId == uid.Value) ||
                          (isParticipant && en.UserId == uid.Value)
                    select result;
        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetResult(int id)
    {
        var uid = UserId(); if (uid == null) return Unauthorized();
        bool isOrganiser = User.IsInRole("Organiser");
        bool isParticipant = User.IsInRole("Participant");
        var result = await (from rr in _db.Results.AsNoTracking()
                            join en in _db.Enrolments on rr.EnrolmentId equals en.EnrolmentId
                            join ev in _db.Events on en.EventId equals ev.EventId
                            where rr.ResultId == id && ((isOrganiser && ev.OrganiserId == uid.Value) ||
                                    (isParticipant && en.UserId == uid.Value))
                            select rr).FirstOrDefaultAsync();
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost, Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateResult([FromBody] RaceDay.Api.Models.Result input)
    {
        if (input.Time <= 0 || input.Position <= 0) return BadRequest("Time and position must be positive.");
        var uid = UserId(); if (uid == null) return Unauthorized();
        var enrolment = await _db.Enrolments.AsNoTracking().FirstOrDefaultAsync(e => e.EnrolmentId == input.EnrolmentId);
        if (enrolment == null) return NotFound("Enrolment not found.");
        var race = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == enrolment.EventId);
        if (race == null) return NotFound("Event not found.");
        if (race.OrganiserId != uid) return Forbid();
        if (enrolment.Status != "Confirmed") return BadRequest("Only confirmed enrolments can receive results.");
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == input.EnrolmentId)) return Conflict("Result already recorded.");
        input.ResultId = 0; _db.Results.Add(input); await _db.SaveChangesAsync();
        return Created($"/api/results/{input.ResultId}", input);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] RaceDay.Api.Models.Result input)
    {
        if (input.ResultId != id) return BadRequest("Result IDs do not match.");
        if (input.Time <= 0 || input.Position <= 0) return BadRequest("Time and position must be positive.");
        var uid = UserId(); if (uid == null) return Unauthorized();
        var result = await _db.Results.FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound();
        if (result.EnrolmentId != input.EnrolmentId) return BadRequest("Result enrolment cannot be changed.");
        var en = await _db.Enrolments.FindAsync(result.EnrolmentId);
        if (en == null) return NotFound();
        var ev = await _db.Events.FindAsync(en.EventId);
        if (ev?.OrganiserId != uid) return Forbid();
        if (en.Status != "Confirmed") return BadRequest("Only confirmed enrolments can receive results.");
        result.Time = input.Time; result.Position = input.Position;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteResult(int id)
    {
        var uid = UserId(); if (uid == null) return Unauthorized();
        var result = await _db.Results.FindAsync(id);
        if (result == null) return NotFound();
        var en = await _db.Enrolments.FindAsync(result.EnrolmentId);
        if (en == null) return NotFound();
        var ev = await _db.Events.FindAsync(en.EventId);
        if (ev?.OrganiserId != uid) return Forbid();
        _db.Results.Remove(result); await _db.SaveChangesAsync(); return NoContent();
    }
}
