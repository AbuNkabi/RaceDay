
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // GET: api/Results
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RaceDay.Api.Models.Result>>> GetResults()
    {
        var results = await _context.Results
            .AsNoTracking()
            .ToListAsync();

        return Ok(results);
    }

    // GET: api/Results/5
    [HttpGet("{id}")]
    public async Task<ActionResult<RaceDay.Api.Models.Result>> GetResult(int id)
    {
        var result = await _context.Results.FindAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // POST: api/Results
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<RaceDay.Api.Models.Result>> CreateResult(
        RaceDay.Api.Models.Result result)
    {
        if (result.Time <= 0 || result.Position <= 0)
        {
            return BadRequest(
                "Time and position must be greater than zero.");
        }

        var enrolmentExists = await _context.Enrolments
            .AnyAsync(e => e.EnrolmentId == result.EnrolmentId);

        if (!enrolmentExists)
        {
            return BadRequest(
                "The specified enrolment does not exist.");
        }

        result.ResultId = 0;

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetResult),
            new { id = result.ResultId },
            result);
    }

    // PUT: api/Results/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateResult(
        int id, RaceDay.Api.Models.Result result)
    {
        if (id != result.ResultId)
        {
            return BadRequest(
                "The ID in the URL must match ResultId.");
        }

        if (result.Time <= 0 || result.Position <= 0)
        {
            return BadRequest(
                "Time and position must be greater than zero.");
        }

        var existingResult = await _context.Results.FindAsync(id);

        if (existingResult == null)
            return NotFound();

        var enrolmentExists = await _context.Enrolments
            .AnyAsync(e => e.EnrolmentId == result.EnrolmentId);

        if (!enrolmentExists)
        {
            return BadRequest(
                "The specified enrolment does not exist.");
        }

        existingResult.EnrolmentId = result.EnrolmentId;
        existingResult.Time = result.Time;
        existingResult.Position = result.Position;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Results/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteResult(int id)
    {
        var result = await _context.Results.FindAsync(id);

        if (result == null)
            return NotFound();

        _context.Results.Remove(result);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
