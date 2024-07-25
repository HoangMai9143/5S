using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnswerModel3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_Question_QuestionId",
                table: "Answers");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Answers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "QuestionId",
                table: "Answers",
                newName: "question_id");

            migrationBuilder.RenameIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                newName: "IX_Answers_question_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_Question_question_id",
                table: "Answers",
                column: "question_id",
                principalTable: "Question",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_Question_question_id",
                table: "Answers");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Answers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "question_id",
                table: "Answers",
                newName: "QuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Answers_question_id",
                table: "Answers",
                newName: "IX_Answers_QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_Question_QuestionId",
                table: "Answers",
                column: "QuestionId",
                principalTable: "Question",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
