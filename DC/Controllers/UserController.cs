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
    public ActionResult<IEnumerable<UserAccountModel>> GetAll()
    {
      return _context.UserAccountModel;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserAccountModel?>> GetById(int id)
    {
      return await _context.UserAccountModel.Where(x => x.Id == id).SingleOrDefaultAsync();
    }

    [HttpPost]
    public async Task<ActionResult> Create(UserAccountModel user)
    {

      await _context.UserAccountModel.AddAsync(user);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UserAccountModel user)
    {
      if (id != user.Id)
        return BadRequest();

      // Ensure the CreatedDate is not modified
      var existingUser = await _context.UserAccountModel.FindAsync(id);
      if (existingUser == null)
        return NotFound();


      _context.Entry(existingUser).CurrentValues.SetValues(user);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteById(int id)
    {
      var user = await _context.UserAccountModel.FindAsync(id);
      if (user == null)
        return NotFound();

      _context.UserAccountModel.Remove(user);
      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}