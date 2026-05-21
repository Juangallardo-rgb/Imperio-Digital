using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimuladorApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMethodologyCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MethodologyId",
                table: "Scenarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomName",
                table: "ScenarioPhaseSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "ScenarioPhaseSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MethodologyPhaseId",
                table: "ScenarioPhaseSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MethodologyPhaseId",
                table: "ScenarioOptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MethodologyPhaseCriteriaId",
                table: "PhaseCriteriaSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Methodologies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Methodologies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MethodologyPhases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MethodologyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PhaseOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DefaultWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    DefaultMaxSelections = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodologyPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodologyPhases_Methodologies_MethodologyId",
                        column: x => x.MethodologyId,
                        principalTable: "Methodologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodologyPhaseCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MethodologyPhaseId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DefaultWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EvaluationType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodologyPhaseCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodologyPhaseCriteria_MethodologyPhases_MethodologyPhase~",
                        column: x => x.MethodologyPhaseId,
                        principalTable: "MethodologyPhases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scenarios_MethodologyId",
                table: "Scenarios",
                column: "MethodologyId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioPhaseSettings_MethodologyPhaseId",
                table: "ScenarioPhaseSettings",
                column: "MethodologyPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioOptions_MethodologyPhaseId",
                table: "ScenarioOptions",
                column: "MethodologyPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseCriteriaSettings_MethodologyPhaseCriteriaId",
                table: "PhaseCriteriaSettings",
                column: "MethodologyPhaseCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Methodologies_Code",
                table: "Methodologies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodologyPhaseCriteria_MethodologyPhaseId",
                table: "MethodologyPhaseCriteria",
                column: "MethodologyPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodologyPhases_MethodologyId_PhaseOrder",
                table: "MethodologyPhases",
                columns: new[] { "MethodologyId", "PhaseOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PhaseCriteriaSettings_MethodologyPhaseCriteria_MethodologyP~",
                table: "PhaseCriteriaSettings",
                column: "MethodologyPhaseCriteriaId",
                principalTable: "MethodologyPhaseCriteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScenarioOptions_MethodologyPhases_MethodologyPhaseId",
                table: "ScenarioOptions",
                column: "MethodologyPhaseId",
                principalTable: "MethodologyPhases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScenarioPhaseSettings_MethodologyPhases_MethodologyPhaseId",
                table: "ScenarioPhaseSettings",
                column: "MethodologyPhaseId",
                principalTable: "MethodologyPhases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scenarios_Methodologies_MethodologyId",
                table: "Scenarios",
                column: "MethodologyId",
                principalTable: "Methodologies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhaseCriteriaSettings_MethodologyPhaseCriteria_MethodologyP~",
                table: "PhaseCriteriaSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_ScenarioOptions_MethodologyPhases_MethodologyPhaseId",
                table: "ScenarioOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ScenarioPhaseSettings_MethodologyPhases_MethodologyPhaseId",
                table: "ScenarioPhaseSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Scenarios_Methodologies_MethodologyId",
                table: "Scenarios");

            migrationBuilder.DropTable(
                name: "MethodologyPhaseCriteria");

            migrationBuilder.DropTable(
                name: "MethodologyPhases");

            migrationBuilder.DropTable(
                name: "Methodologies");

            migrationBuilder.DropIndex(
                name: "IX_Scenarios_MethodologyId",
                table: "Scenarios");

            migrationBuilder.DropIndex(
                name: "IX_ScenarioPhaseSettings_MethodologyPhaseId",
                table: "ScenarioPhaseSettings");

            migrationBuilder.DropIndex(
                name: "IX_ScenarioOptions_MethodologyPhaseId",
                table: "ScenarioOptions");

            migrationBuilder.DropIndex(
                name: "IX_PhaseCriteriaSettings_MethodologyPhaseCriteriaId",
                table: "PhaseCriteriaSettings");

            migrationBuilder.DropColumn(
                name: "MethodologyId",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "CustomName",
                table: "ScenarioPhaseSettings");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "ScenarioPhaseSettings");

            migrationBuilder.DropColumn(
                name: "MethodologyPhaseId",
                table: "ScenarioPhaseSettings");

            migrationBuilder.DropColumn(
                name: "MethodologyPhaseId",
                table: "ScenarioOptions");

            migrationBuilder.DropColumn(
                name: "MethodologyPhaseCriteriaId",
                table: "PhaseCriteriaSettings");
        }
    }
}
