using System;
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
  public class UserController : ControllerBase
  {
    private readonly AppDbContext _context;
    public UserController(AppDbContext context) => _context = context;

    [HttpGet]
    public ActionResult<IEnumerable<User>> Get()
    {
      return _context.User;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User?>> GetById(int id)
    {
      return await _context.User.Where(x => x.id == id).SingleOrDefaultAsync();
    }

    [HttpPost]
    public async Task<ActionResult> Create(User user)
    {
      user.CreatedAt = DateTime.UtcNow;

      await _context.User.AddAsync(user);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = user.id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, User user)
    {
      if (id != user.id)
        return BadRequest();

      // Ensure the CreatedDate is not modified
      var existingUser = await _context.User.FindAsync(id);
      if (existingUser == null)
        return NotFound();

      user.CreatedAt = existingUser.CreatedAt;

      _context.Entry(existingUser).CurrentValues.SetValues(user);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
      var user = await _context.User.FindAsync(id);
      if (user == null)
        return NotFound();

      _context.User.Remove(user);
      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}