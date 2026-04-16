using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimuladorApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scenarios_Users_CreatedByUserId",
                table: "Scenarios");

            migrationBuilder.CreateTable(
                name: "Simulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ScenarioId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DigitalMaturity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OperationalEfficiency = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustomerExperience = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GlobalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FeedbackRule = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Simulations_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Simulations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SimulationVariableValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SimulationId = table.Column<int>(type: "int", nullable: false),
                    ScenarioVariableId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationVariableValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationVariableValues_ScenarioVariables_ScenarioVariableId",
                        column: x => x.ScenarioVariableId,
                        principalTable: "ScenarioVariables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationVariableValues_Simulations_SimulationId",
                        column: x => x.SimulationId,
                        principalTable: "Simulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_ScenarioId",
                table: "Simulations",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_UserId",
                table: "Simulations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationVariableValues_ScenarioVariableId",
                table: "SimulationVariableValues",
                column: "ScenarioVariableId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationVariableValues_SimulationId",
                table: "SimulationVariableValues",
                column: "SimulationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scenarios_Users_CreatedByUserId",
                table: "Scenarios",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scenarios_Users_CreatedByUserId",
                table: "Scenarios");

            migrationBuilder.DropTable(
                name: "SimulationVariableValues");

            migrationBuilder.DropTable(
                name: "Simulations");

            migrationBuilder.AddForeignKey(
                name: "FK_Scenarios_Users_CreatedByUserId",
                table: "Scenarios",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
