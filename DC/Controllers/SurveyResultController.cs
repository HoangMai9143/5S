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
    public ActionResult<IEnumerable<SurveyResultModel>> GetAll()
    {
      return _context.SurveyResultModel;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SurveyResultModel?>> GetById(int id)
    {
      return await _context.SurveyResultModel.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(SurveyResultModel surveyResult)
    {
      await _context.SurveyResultModel.AddAsync(surveyResult);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = surveyResult.Id }, surveyResult);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SurveyResultModel surveyResult)
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
    public async Task<ActionResult> DeleteById(int id)
    {
      var surveyResult = await _context.SurveyResultModel.FindAsync(id);
      if (surveyResult == null)
        return NotFound();

      _context.SurveyResultModel.Remove(surveyResult);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool SurveyResultExists(int id)
    {
      return _context.SurveyResultModel.Any(e => e.Id == id);
    }
  }
}