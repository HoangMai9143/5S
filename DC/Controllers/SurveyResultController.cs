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
  public class SurveyResultController : ControllerBase
  {
    private readonly AppDbContext _context;
    public SurveyResultController(AppDbContext context) => _context = context;

    [HttpGet]
    public ActionResult<IEnumerable<SurveyResult>> Get()
    {
      return _context.SurveyResult;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SurveyResult?>> GetById(int id)
    {
      return await _context.SurveyResult.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(SurveyResult surveyResult)
    {
      await _context.SurveyResult.AddAsync(surveyResult);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = surveyResult.Id }, surveyResult);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SurveyResult surveyResult)
    {
      if (id != surveyResult.Id)
        return BadRequest();

      _context.Entry(surveyResult).State = EntityState.Modified;

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!SurveyResultExists(id))
          return NotFound();
        else
          throw;
      }

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
      var surveyResult = await _context.SurveyResult.FindAsync(id);
      if (surveyResult == null)
        return NotFound();

      _context.SurveyResult.Remove(surveyResult);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool SurveyResultExists(int id)
    {
      return _context.SurveyResult.Any(e => e.Id == id);
    }
  }
}