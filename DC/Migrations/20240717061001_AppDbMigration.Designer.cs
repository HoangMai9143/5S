﻿// <auto-generated />
using System;
using DC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DC.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240717061001_AppDbMigration")]
    partial class AppDbMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("DC.Models.Question", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("QuestionContext")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("question_context");

                    b.HasKey("Id");

                    b.ToTable("question");
                });

            modelBuilder.Entity("DC.Models.Result", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("Grade")
                        .HasColumnType("int")
                        .HasColumnName("grade");

                    b.Property<string>("Note")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("note");

                    b.Property<int>("QuestionId")
                        .HasColumnType("int")
                        .HasColumnName("question_id");

                    b.Property<int>("SurveyId")
                        .HasColumnType("int")
                        .HasColumnName("survey_id");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("QuestionId");

                    b.HasIndex("SurveyId");

                    b.HasIndex("UserId");

                    b.ToTable("result");
                });

            modelBuilder.Entity("DC.Models.Survey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("created_date");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("end_date");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit")
                        .HasColumnName("isActive");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("start_date");

                    b.HasKey("Id");

                    b.ToTable("survey");
                });

            modelBuilder.Entity("DC.Models.SurveyQuestion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("QuestionId")
                        .HasColumnType("int")
                        .HasColumnName("question_id");

                    b.Property<int>("SurveyId")
                        .HasColumnType("int")
                        .HasColumnName("survey_id");

                    b.HasKey("Id");

                    b.HasIndex("QuestionId");

                    b.HasIndex("SurveyId");

                    b.ToTable("survey_question");
                });

            modelBuilder.Entity("DC.Models.SurveyResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("FinalGrade")
                        .HasColumnType("int")
                        .HasColumnName("final_grade");

                    b.Property<string>("Note")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("note");

                    b.Property<int>("SurveyId")
                        .HasColumnType("int")
                        .HasColumnName("survey_id");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("SurveyId");

                    b.HasIndex("UserId");

                    b.ToTable("survey_result");
                });

            modelBuilder.Entity("DC.Models.User", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("created_date");

                    b.Property<string>("Department")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("department");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit")
                        .HasColumnName("isActive");

                    b.Property<string>("Password")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("Role")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("role");

                    b.Property<string>("Username")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("username");

                    b.HasKey("id");

                    b.ToTable("user");
                });

            modelBuilder.Entity("DC.Models.Result", b =>
                {
                    b.HasOne("DC.Models.Question", "Question")
                        .WithMany()
                        .HasForeignKey("QuestionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DC.Models.Survey", "Survey")
                        .WithMany()
                        .HasForeignKey("SurveyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DC.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Question");

                    b.Navigation("Survey");

                    b.Navigation("User");
                });

            modelBuilder.Entity("DC.Models.SurveyQuestion", b =>
                {
                    b.HasOne("DC.Models.Question", "Question")
                        .WithMany()
                        .HasForeignKey("QuestionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DC.Models.Survey", "Survey")
                        .WithMany()
                        .HasForeignKey("SurveyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Question");

                    b.Navigation("Survey");
                });

            modelBuilder.Entity("DC.Models.SurveyResult", b =>
                {
                    b.HasOne("DC.Models.Survey", "Survey")
                        .WithMany()
                        .HasForeignKey("SurveyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DC.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Survey");

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}