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
  public class SurveyController : ControllerBase
  {
    private readonly AppDbContext _context;
    public SurveyController(AppDbContext context) => _context = context;

    [HttpGet]
    public ActionResult<IEnumerable<Survey>> Get()
    {
      return _context.Survey;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Survey?>> GetById(int id)
    {
      return await _context.Survey.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Survey survey)
    {
      survey.CreatedAt = DateTime.UtcNow;

      await _context.Survey.AddAsync(survey);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = survey.Id }, survey);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Survey survey)
    {
      if (id != survey.Id)
        return BadRequest();

      var existingSurvey = await _context.Survey.FindAsync(id);
      if (existingSurvey == null)
        return NotFound();

      survey.CreatedAt = existingSurvey.CreatedAt;

      _context.Entry(existingSurvey).CurrentValues.SetValues(survey);

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!SurveyExists(id))
          return NotFound();
        else
          throw;
      }

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
      var survey = await _context.Survey.FindAsync(id);
      if (survey == null)
        return NotFound();

      _context.Survey.Remove(survey);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool SurveyExists(int id)
    {
      return _context.Survey.Any(e => e.Id == id);
    }
  }
}