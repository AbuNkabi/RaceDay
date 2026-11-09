 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
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

        // GET: api/Enrolments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Enrolment>>> GetEnrolments()
        {
            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET: api/Enrolments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Enrolment>> GetEnrolment(int id)
        {
            var enrolment = await _context.Enrolments.FindAsync(id);

            if (enrolment == null)
            {
                return NotFound();
            }

            return Ok(enrolment);
        }

        // POST: api/Enrolments
        [HttpPost]
        public async Task<ActionResult<Enrolment>> CreateEnrolment(
            Enrolment enrolment)
        {
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
            if (id != enrolment.EnrolmentId)
            {
                return BadRequest(
                    "The ID in the URL must match EnrolmentId.");
            }

            var existingEnrolment =
                await _context.Enrolments.FindAsync(id);

            if (existingEnrolment == null)
            {
                return NotFound();
            }

            _context.Entry(existingEnrolment)
                .CurrentValues.SetValues(enrolment);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Enrolments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnrolment(int id)
        {
            var enrolment = await _context.Enrolments.FindAsync(id);

            if (enrolment == null)
            {
                return NotFound();
            }

            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

