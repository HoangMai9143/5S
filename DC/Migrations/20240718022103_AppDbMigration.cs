using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DC.Migrations
{
    /// <inheritdoc />
    public partial class AppDbMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    question_context = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    department = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyModel",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: false),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyModel", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccountModel",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    role = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccountModel", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ResultModel",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    survey_id = table.Column<int>(type: "int", nullable: false),
                    staff_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    grade = table.Column<int>(type: "int", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultModel", x => x.id);
                    table.ForeignKey(
                        name: "FK_ResultModel_QuestionModel_question_id",
                        column: x => x.question_id,
                        principalTable: "QuestionModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultModel_StaffModel_staff_id",
                        column: x => x.staff_id,
                        principalTable: "StaffModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultModel_SurveyModel_survey_id",
                        column: x => x.survey_id,
                        principalTable: "SurveyModel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestionModel",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    survey_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestionModel", x => x.id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestionModel_QuestionModel_question_id",
                        column: x => x.question_id,
                        principalTable: "QuestionModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SurveyQuestionModel_SurveyModel_survey_id",
                        column: x => x.survey_id,
                        principalTable: "SurveyModel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyResultModel",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    survey_id = table.Column<int>(type: "int", nullable: false),
                    staff_id = table.Column<int>(type: "int", nullable: false),
                    final_grade = table.Column<int>(type: "int", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResultModel", x => x.id);
                    table.ForeignKey(
                        name: "FK_SurveyResultModel_StaffModel_staff_id",
                        column: x => x.staff_id,
                        principalTable: "StaffModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SurveyResultModel_SurveyModel_survey_id",
                        column: x => x.survey_id,
                        principalTable: "SurveyModel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResultModel_question_id",
                table: "ResultModel",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_ResultModel_staff_id",
                table: "ResultModel",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_ResultModel_survey_id",
                table: "ResultModel",
                column: "survey_id");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestionModel_question_id",
                table: "SurveyQuestionModel",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestionModel_survey_id",
                table: "SurveyQuestionModel",
                column: "survey_id");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResultModel_staff_id",
                table: "SurveyResultModel",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResultModel_survey_id",
                table: "SurveyResultModel",
                column: "survey_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultModel");

            migrationBuilder.DropTable(
                name: "SurveyQuestionModel");

            migrationBuilder.DropTable(
                name: "SurveyResultModel");

            migrationBuilder.DropTable(
                name: "UserAccountModel");

            migrationBuilder.DropTable(
                name: "QuestionModel");

            migrationBuilder.DropTable(
                name: "StaffModel");

            migrationBuilder.DropTable(
                name: "SurveyModel");
        }
    }
}
