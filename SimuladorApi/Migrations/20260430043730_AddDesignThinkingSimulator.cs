using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimuladorApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignThinkingSimulator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyType",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Constraints",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Scenarios",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Scenarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Methodology",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Problem",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetUser",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Scenarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScenarioOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScenarioId = table.Column<int>(type: "integer", nullable: false),
                    PhaseName = table.Column<string>(type: "text", nullable: false),
                    OptionType = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    ImpactJson = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioOptions_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioPhaseSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScenarioId = table.Column<int>(type: "integer", nullable: false),
                    PhaseName = table.Column<string>(type: "text", nullable: false),
                    PhaseOrder = table.Column<int>(type: "integer", nullable: false),
                    PhaseWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioPhaseSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioPhaseSettings_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScenarioId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    FinalFeedback = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationAttempts_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationAttempts_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhaseCriteriaSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScenarioPhaseSettingId = table.Column<int>(type: "integer", nullable: false),
                    CriterionName = table.Column<string>(type: "text", nullable: false),
                    CriterionWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EvaluationType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseCriteriaSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhaseCriteriaSettings_ScenarioPhaseSettings_ScenarioPhaseSe~",
                        column: x => x.ScenarioPhaseSettingId,
                        principalTable: "ScenarioPhaseSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationKpiResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationAttemptId = table.Column<int>(type: "integer", nullable: false),
                    KpiName = table.Column<string>(type: "text", nullable: false),
                    InitialValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FinalValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationKpiResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationKpiResults_SimulationAttempts_SimulationAttemptId",
                        column: x => x.SimulationAttemptId,
                        principalTable: "SimulationAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationPhaseResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationAttemptId = table.Column<int>(type: "integer", nullable: false),
                    PhaseName = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationPhaseResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationPhaseResponses_SimulationAttempts_SimulationAttem~",
                        column: x => x.SimulationAttemptId,
                        principalTable: "SimulationAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationPhaseResponseId = table.Column<int>(type: "integer", nullable: false),
                    QuestionType = table.Column<string>(type: "text", nullable: false),
                    SelectedOptionIdsJson = table.Column<string>(type: "text", nullable: false),
                    TextAnswer = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationAnswers_SimulationPhaseResponses_SimulationPhaseR~",
                        column: x => x.SimulationPhaseResponseId,
                        principalTable: "SimulationPhaseResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhaseCriteriaSettings_ScenarioPhaseSettingId",
                table: "PhaseCriteriaSettings",
                column: "ScenarioPhaseSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioOptions_ScenarioId",
                table: "ScenarioOptions",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioPhaseSettings_ScenarioId",
                table: "ScenarioPhaseSettings",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationAnswers_SimulationPhaseResponseId",
                table: "SimulationAnswers",
                column: "SimulationPhaseResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationAttempts_ScenarioId",
                table: "SimulationAttempts",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationAttempts_StudentId",
                table: "SimulationAttempts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationKpiResults_SimulationAttemptId",
                table: "SimulationKpiResults",
                column: "SimulationAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationPhaseResponses_SimulationAttemptId",
                table: "SimulationPhaseResponses",
                column: "SimulationAttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhaseCriteriaSettings");

            migrationBuilder.DropTable(
                name: "ScenarioOptions");

            migrationBuilder.DropTable(
                name: "SimulationAnswers");

            migrationBuilder.DropTable(
                name: "SimulationKpiResults");

            migrationBuilder.DropTable(
                name: "ScenarioPhaseSettings");

            migrationBuilder.DropTable(
                name: "SimulationPhaseResponses");

            migrationBuilder.DropTable(
                name: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "CompanyType",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "Constraints",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "Methodology",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "Problem",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "TargetUser",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Scenarios");
        }
    }
}
