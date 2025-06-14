using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutorial11.DAL;
using Tutorial11.Models;

namespace Tutorial11.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PositionsController : ControllerBase
    {
        private readonly DeviceContext _context;

        public PositionsController(DeviceContext context)
        {
            _context = context;
        }

        // GET: api/Positions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Position>>> GetPosition()
        {
            try
            {
                var devices = await _context.Position
                    .Select(d => new { d.Id, d.Name })
                    .ToListAsync();

                return Ok(devices);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Server error", instance: "api/positions");
            }
        }

        // GET: api/Positions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Position>> GetPosition(int id)
        {
            var position = await _context.Position.FindAsync(id);

            if (position == null)
            {
                return NotFound();
            }

            return position;
        }

        // PUT: api/Positions/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPosition(int id, Position position)
        {
            if (id != position.Id)
            {
                return BadRequest();
            }

            _context.Entry(position).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PositionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Positions
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Position>> PostPosition(Position position)
        {
            _context.Position.Add(position);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPosition", new { id = position.Id }, position);
        }

        // DELETE: api/Positions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            var position = await _context.Position.FindAsync(id);
            if (position == null)
            {
                return NotFound();
            }

            _context.Position.Remove(position);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PositionExists(int id)
        {
            return _context.Position.Any(e => e.Id == id);
        }
    }
}
