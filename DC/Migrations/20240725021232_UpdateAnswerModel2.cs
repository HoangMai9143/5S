using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnswerModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Points",
                table: "Answers",
                newName: "points");

            migrationBuilder.RenameColumn(
                name: "AnswerType",
                table: "Answers",
                newName: "answer_type");

            migrationBuilder.RenameColumn(
                name: "AnswerText",
                table: "Answers",
                newName: "answer_text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "points",
                table: "Answers",
                newName: "Points");

            migrationBuilder.RenameColumn(
                name: "answer_type",
                table: "Answers",
                newName: "AnswerType");

            migrationBuilder.RenameColumn(
                name: "answer_text",
                table: "Answers",
                newName: "AnswerText");
        }
    }
}
