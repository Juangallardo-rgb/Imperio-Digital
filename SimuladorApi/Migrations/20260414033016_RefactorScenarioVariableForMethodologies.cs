using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimuladorApi.Migrations
{
    /// <inheritdoc />
    public partial class RefactorScenarioVariableForMethodologies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Methodology",
                table: "ScenarioVariables",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "ScenarioVariables",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetKpi",
                table: "ScenarioVariables",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "ScenarioVariables",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Methodology",
                table: "ScenarioVariables");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "ScenarioVariables");

            migrationBuilder.DropColumn(
                name: "TargetKpi",
                table: "ScenarioVariables");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "ScenarioVariables");
        }
    }
}
