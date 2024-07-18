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
  public class ResultController : ControllerBase
  {
    private readonly AppDbContext _context;
    public ResultController(AppDbContext context) => _context = context;

    [HttpGet]
    public ActionResult<IEnumerable<ResultModel>> GetAll()
    {
      return _context.ResultModel;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResultModel?>> GetById(int id)
    {
      return await _context.ResultModel.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(ResultModel result)
    {
      await _context.ResultModel.AddAsync(result);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, ResultModel result)
    {
      if (id != result.Id)
        return BadRequest();

      _context.Entry(result).State = EntityState.Modified;

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!ResultExists(id))
          return NotFound();
        else
          throw;
      }

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteById(int id)
    {
      var result = await _context.ResultModel.FindAsync(id);
      if (result == null)
        return NotFound();

      _context.ResultModel.Remove(result);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool ResultExists(int id)
    {
      return _context.ResultModel.Any(e => e.Id == id);
    }
  }
}