using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimuladorApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationResourcesAndOptionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentKpisJson",
                table: "SimulationAttempts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentPhase",
                table: "SimulationAttempts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DecisionTraceJson",
                table: "SimulationAttempts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "InitialBudget",
                table: "SimulationAttempts",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InitialKpisJson",
                table: "SimulationAttempts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "InitialTimeWeeks",
                table: "SimulationAttempts",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingBudget",
                table: "SimulationAttempts",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingTimeWeeks",
                table: "SimulationAttempts",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskLevel",
                table: "SimulationAttempts",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TriggeredEventsJson",
                table: "SimulationAttempts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "ScenarioOptions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedEffortLevel",
                table: "ScenarioOptions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedImpactLevel",
                table: "ScenarioOptions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedViabilityLevel",
                table: "ScenarioOptions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxSelections",
                table: "ScenarioOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskImpact",
                table: "ScenarioOptions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "ScenarioOptions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TimeCost",
                table: "ScenarioOptions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentKpisJson",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "CurrentPhase",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "DecisionTraceJson",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "InitialBudget",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "InitialKpisJson",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "InitialTimeWeeks",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "RemainingBudget",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "RemainingTimeWeeks",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "TriggeredEventsJson",
                table: "SimulationAttempts");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "ScenarioOptions");

            migrationBuilder.DropColumn(
                name: "ExpectedEffortLevel",
                table: "ScenarioOptions");

            migrationBuilder.DropColumn(
                name: "ExpectedImpactLevel",
                table: "ScenarioOptions");

            migrationBuilder.DropColumn(
                name: "ExpectedViabilityLevel",
                table: "ScenarioOptions");

            migrationBuilder.DropColumn(
                name: "MaxSelections",
                table: "ScenarioOptions");

            migrationBuilder.DropColumn(
                name: "RiskImpact",
                table: "ScenarioOptions");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "ScenarioOptions");

            migrationBuilder.DropColumn(
                name: "TimeCost",
                table: "ScenarioOptions");
        }
    }
}
