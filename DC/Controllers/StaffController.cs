using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Models;
using DC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DC.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class StaffController : ControllerBase
  {
    private readonly AppDbContext _context;

    public StaffController(AppDbContext context)
    {
      _context = context;
    }

    // GET: api/Staff
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StaffModel>>> GetAll()
    {
      return await _context.StaffModel.ToListAsync();
    }

    // GET: api/Staff/5
    [HttpGet("{id}")]
    public async Task<ActionResult<StaffModel>> GetById(int id)
    {
      var staff = await _context.StaffModel.FindAsync(id);

      if (staff == null)
      {
        return NotFound();
      }

      return staff;
    }

    // POST: api/Staff
    [HttpPost]
    public async Task<ActionResult<StaffModel>> Post(StaffModel staff)
    {
      _context.StaffModel.Add(staff);
      await _context.SaveChangesAsync();

      return CreatedAtAction("GetStaff", new { id = staff.Id }, staff);
    }

    // PUT: api/Staff/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, StaffModel staff)
    {
      if (id != staff.Id)
      {
        return BadRequest();
      }

      _context.Entry(staff).State = EntityState.Modified;

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!StaffExists(id))
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

    // DELETE: api/Staff/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
      var staff = await _context.StaffModel.FindAsync(id);
      if (staff == null)
      {
        return NotFound();
      }

      _context.StaffModel.Remove(staff);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool StaffExists(int id)
    {
      return _context.StaffModel.Any(e => e.Id == id);
    }
  }
}