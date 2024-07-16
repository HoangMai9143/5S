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
    public async Task<ActionResult<UserAccount?>> GetById(int ID)
    {
      return await _context.UserAccounts.Where(x => x.ID == ID).SingleOrDefaultAsync();
    }

    [HttpPost]
    public async Task<ActionResult> Create(UserAccount userAccount)
    {
      await _context.UserAccounts.AddAsync(userAccount);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { ID = userAccount.ID }, userAccount);
    }

    [HttpPut]
    public async Task<ActionResult> Update(UserAccount userAccount)
    {
      _context.UserAccounts.Update(userAccount);
      await _context.SaveChangesAsync();

      return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int ID)
    {
      var userAccountGetById = await GetById(ID);
      if (userAccountGetById.Value is null)
        return NotFound();

      _context.Remove(userAccountGetById.Value);
      await _context.SaveChangesAsync();

      return Ok();
    }

  }
}