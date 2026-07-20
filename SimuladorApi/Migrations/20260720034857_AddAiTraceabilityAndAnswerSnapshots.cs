using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimuladorApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTraceabilityAndAnswerSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedOptionsSnapshotJson",
                table: "SimulationAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TextEvaluatedAt",
                table: "SimulationAnswers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextEvaluationJson",
                table: "SimulationAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TextEvaluationModel",
                table: "SimulationAnswers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextEvaluationPromptVersion",
                table: "SimulationAnswers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextEvaluationProvider",
                table: "SimulationAnswers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextEvaluationStatus",
                table: "SimulationAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AiGeneratedAt",
                table: "Scenarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModel",
                table: "Scenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiPromptVersion",
                table: "Scenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiProvider",
                table: "Scenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreationMode",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<bool>(
                name: "GeneratedByAi",
                table: "Scenarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AiGenerationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScenarioId = table.Column<int>(type: "integer", nullable: true),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    OperationType = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    RequestedModel = table.Column<string>(type: "text", nullable: false),
                    EffectiveModel = table.Column<string>(type: "text", nullable: true),
                    PromptVersion = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptHash = table.Column<string>(type: "text", nullable: true),
                    ResponseHash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiGenerationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiGenerationRecords_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationRecords_CorrelationId",
                table: "AiGenerationRecords",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationRecords_ScenarioId",
                table: "AiGenerationRecords",
                column: "ScenarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiGenerationRecords");

            migrationBuilder.DropColumn(
                name: "SelectedOptionsSnapshotJson",
                table: "SimulationAnswers");

            migrationBuilder.DropColumn(
                name: "TextEvaluatedAt",
                table: "SimulationAnswers");

            migrationBuilder.DropColumn(
                name: "TextEvaluationJson",
                table: "SimulationAnswers");

            migrationBuilder.DropColumn(
                name: "TextEvaluationModel",
                table: "SimulationAnswers");

            migrationBuilder.DropColumn(
                name: "TextEvaluationPromptVersion",
                table: "SimulationAnswers");

            migrationBuilder.DropColumn(
                name: "TextEvaluationProvider",
                table: "SimulationAnswers");

            migrationBuilder.DropColumn(
                name: "TextEvaluationStatus",
                table: "SimulationAnswers");

            migrationBuilder.DropColumn(
                name: "AiGeneratedAt",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "AiModel",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "AiPromptVersion",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "AiProvider",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "CreationMode",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "GeneratedByAi",
                table: "Scenarios");
        }
    }
}
