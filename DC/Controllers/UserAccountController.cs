using System.Collections;
using System.Net.Http.Headers;
using DC.Models;
using DC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace DC.Controllers
{
  [Route("api/[controller]")]
  [ApiController]

  public class UserAccountController : ControllerBase
  {
    private readonly AppDbContext _context;
    public UserAccountController(AppDbContext context) => _context = context;

    [HttpGet]
    public ActionResult<IEnumerable<UserAccount>> Get()
    {
      return _context.UserAccounts;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserAccount?>> GetById(int id)
    {
      return await _context.UserAccounts.Where(x => x.Id == id).SingleOrDefaultAsync();
    }

    [HttpPost]
    public async Task<ActionResult> Create(UserAccount userAccount)
    {
      await _context.UserAccounts.AddAsync(userAccount);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = userAccount.Id }, userAccount);
    }

    [HttpPut]
    public async Task<ActionResult> Update(UserAccount userAccount)
    {
      _context.UserAccounts.Update(userAccount);
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