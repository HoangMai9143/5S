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
    public ActionResult<IEnumerable<SurveyModel>> GetAll()
    {
      return _context.SurveyModel;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SurveyModel?>> GetById(int id)
    {
      return await _context.SurveyModel.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(SurveyModel survey)
    {
      survey.CreatedDate = DateTime.UtcNow;

      await _context.SurveyModel.AddAsync(survey);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = survey.Id }, survey);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SurveyModel survey)
    {
      if (id != survey.Id)
        return BadRequest();

      var existingSurvey = await _context.SurveyModel.FindAsync(id);
      if (existingSurvey == null)
        return NotFound();

      survey.CreatedDate = existingSurvey.CreatedDate;

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
    public async Task<ActionResult> DeleteById(int id)
    {
      var survey = await _context.SurveyModel.FindAsync(id);
      if (survey == null)
        return NotFound();

      _context.SurveyModel.Remove(survey);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool SurveyExists(int id)
    {
      return _context.SurveyModel.Any(e => e.Id == id);
    }
  }
}