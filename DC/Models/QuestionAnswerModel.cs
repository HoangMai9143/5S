using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DC.Models
{
    [Table("QuestionAnswer")]
    public record QuestionAnswerModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("survey_id")]
        public int SurveyId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("staff_id")]
        public int StaffId { get; set; }

        [Column("answer_id")]
        public int AnswerId { get; set; }

        [Column("is_selected")]
        public bool IsSelected { get; set; }

        [ForeignKey("SurveyId")]
        public SurveyModel? Survey { get; set; }

        [ForeignKey("QuestionId")]
        [DeleteBehavior(DeleteBehavior.ClientSetNull)]
        public QuestionModel? Question { get; set; }

        [ForeignKey("StaffId")]
        [DeleteBehavior(DeleteBehavior.ClientSetNull)]
        public StaffModel? Staff { get; set; }

        [ForeignKey("AnswerId")]
        [DeleteBehavior(DeleteBehavior.ClientSetNull)]
        public AnswerModel? Answer { get; set;}
    }
}