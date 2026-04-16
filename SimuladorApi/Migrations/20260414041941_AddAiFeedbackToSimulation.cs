using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimuladorApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFeedbackToSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiFeedback",
                table: "Simulations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiFeedback",
                table: "Simulations");
        }
    }
}
