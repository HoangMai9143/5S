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
  public class QuestionController : ControllerBase
  {
    private readonly AppDbContext _context;
    public QuestionController(AppDbContext context) => _context = context;

    [HttpGet]
    public ActionResult<IEnumerable<Question>> Get()
    {
      return _context.Question;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Question?>> GetById(int id)
    {
      return await _context.Question.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Question question)
    {
      await _context.Question.AddAsync(question);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Question question)
    {
      if (id != question.Id)
        return BadRequest();

      _context.Entry(question).State = EntityState.Modified;

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!QuestionExists(id))
          return NotFound();
        else
          throw;
      }

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
      var question = await _context.Question.FindAsync(id);
      if (question == null)
        return NotFound();

      _context.Question.Remove(question);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool QuestionExists(int id)
    {
      return _context.Question.Any(e => e.Id == id);
    }
  }
}