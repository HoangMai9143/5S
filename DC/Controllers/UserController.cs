using System.Collections;
using System.Net.Http.Headers;
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
      await _context.User.AddAsync(user);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { ID = user.id }, user);
    }

    [HttpPut]
    public async Task<ActionResult> Update(User user)
    {
      _context.User.Update(user);
      await _context.SaveChangesAsync();

      return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
      var userAccountGetById = await GetById(id);
      if (userAccountGetById.Value is null)
        return NotFound();

      _context.Remove(userAccountGetById.Value);
      await _context.SaveChangesAsync();

      return Ok();
    }

  }
}