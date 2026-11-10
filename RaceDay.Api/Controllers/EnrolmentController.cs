
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userId, out int id))
        {
            return id;
        }

        return null;
    }

    // GET: api/Enrolments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Enrolment>>> GetEnrolments()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.UserId == userId.Value)
            .ToListAsync();

        return Ok(enrolments);
    }

    // GET: api/Enrolments/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Enrolment>> GetEnrolment(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var enrolment = await _context.Enrolments
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.EnrolmentId == id && e.UserId == userId.Value);

        if (enrolment == null)
            return NotFound();

        return Ok(enrolment);
    }

    // POST: api/Enrolments
    [HttpPost]
    public async Task<ActionResult<Enrolment>> CreateEnrolment(
        Enrolment enrolment)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == enrolment.EventId);

        if (!eventExists)
            return BadRequest("The selected event does not exist.");

        enrolment.EnrolmentId = 0;
        enrolment.UserId = userId.Value;

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEnrolment),
            new { id = enrolment.EnrolmentId },
            enrolment);
    }

    // PUT: api/Enrolments/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEnrolment(
        int id, Enrolment enrolment)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var existingEnrolment = await _context.Enrolments
            .FirstOrDefaultAsync(e =>
                e.EnrolmentId == id && e.UserId == userId.Value);

        if (existingEnrolment == null)
            return NotFound();

        if (id != enrolment.EnrolmentId)
            return BadRequest("The URL ID must match EnrolmentId.");

        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == enrolment.EventId);

        if (!eventExists)
            return BadRequest("The selected event does not exist.");

        existingEnrolment.EventId = enrolment.EventId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Enrolments/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEnrolment(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var enrolment = await _context.Enrolments
            .FirstOrDefaultAsync(e =>
                e.EnrolmentId == id && e.UserId == userId.Value);

        if (enrolment == null)
            return NotFound();

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
