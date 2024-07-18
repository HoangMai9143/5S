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
    public ActionResult<IEnumerable<Models.QuestionModel>> GetAll()
    {
      return _context.QuestionModel;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Models.QuestionModel?>> GetById(int id)
    {
      return await _context.QuestionModel.FindAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Models.QuestionModel question)
    {
      await _context.QuestionModel.AddAsync(question);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Models.QuestionModel question)
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
    public async Task<ActionResult> DeleteById(int id)
    {
      var question = await _context.QuestionModel.FindAsync(id);
      if (question == null)
        return NotFound();

      _context.QuestionModel.Remove(question);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool QuestionExists(int id)
    {
      return _context.QuestionModel.Any(e => e.Id == id);
    }

    [HttpPost("AssignQuestionToSurvey/{questionId}/{surveyId}")]
    public async Task<ActionResult> AssignQuestionToSurvey(int questionId, int surveyId)
    {
      var questionExists = await _context.QuestionModel.AnyAsync(q => q.Id == questionId);
      var surveyExists = await _context.SurveyModel.AnyAsync(s => s.Id == surveyId);

      if (!questionExists || !surveyExists)
      {
        return NotFound();
      }

      await AddQuestionToSurvey(surveyId, questionId);

      await _context.SaveChangesAsync();

      return Ok();
    }

    private async Task AddQuestionToSurvey(int surveyId, int questionId)
    {
      var surveyQuestion = new SurveyQuestionModel
      {
        SurveyId = surveyId,
        QuestionId = questionId
      };

      await _context.SurveyQuestionModel.AddAsync(surveyQuestion);
    }
  }
}