using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DC.Migrations
{
    /// <inheritdoc />
    public partial class QuestionAnswerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionAnswer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    question_id = table.Column<int>(type: "int", maxLength: 255, nullable: true),
                    answer_id = table.Column<int>(type: "int", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionAnswer_Answers_answer_id",
                        column: x => x.answer_id,
                        principalTable: "Answers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_QuestionAnswer_Question_question_id",
                        column: x => x.question_id,
                        principalTable: "Question",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAnswer_answer_id",
                table: "QuestionAnswer",
                column: "answer_id");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAnswer_question_id",
                table: "QuestionAnswer",
                column: "question_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionAnswer");
        }
    }
}
