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
    public ActionResult<IEnumerable<SurveyQuestionModel>> GetAll()
    {
      return _context.SurveyQuestionModel;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SurveyQuestionModel?>> GetById(int id)
    {
      return await _context.SurveyQuestionModel.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(SurveyQuestionModel surveyQuestion)
    {
      await _context.SurveyQuestionModel.AddAsync(surveyQuestion);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = surveyQuestion.Id }, surveyQuestion);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SurveyQuestionModel surveyQuestion)
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
    public async Task<ActionResult> DeleteById(int id)
    {
      var surveyQuestion = await _context.SurveyQuestionModel.FindAsync(id);
      if (surveyQuestion == null)
        return NotFound();

      _context.SurveyQuestionModel.Remove(surveyQuestion);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool SurveyQuestionExists(int id)
    {
      return _context.SurveyQuestionModel.Any(e => e.Id == id);
    }
  }
}