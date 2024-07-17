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
  public class SurveyQuestionController : ControllerBase
  {
    private readonly AppDbContext _context;
    public SurveyQuestionController(AppDbContext context) => _context = context;

    [HttpGet]
    public ActionResult<IEnumerable<SurveyQuestion>> Get()
    {
      return _context.SurveyQuestion;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SurveyQuestion?>> GetById(int id)
    {
      return await _context.SurveyQuestion.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(SurveyQuestion surveyQuestion)
    {
      await _context.SurveyQuestion.AddAsync(surveyQuestion);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = surveyQuestion.Id }, surveyQuestion);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SurveyQuestion surveyQuestion)
    {
      if (id != surveyQuestion.Id)
        return BadRequest();

      _context.Entry(surveyQuestion).State = EntityState.Modified;

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!SurveyQuestionExists(id))
          return NotFound();
        else
          throw;
      }

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
      var surveyQuestion = await _context.SurveyQuestion.FindAsync(id);
      if (surveyQuestion == null)
        return NotFound();

      _context.SurveyQuestion.Remove(surveyQuestion);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool SurveyQuestionExists(int id)
    {
      return _context.SurveyQuestion.Any(e => e.Id == id);
    }
  }
}