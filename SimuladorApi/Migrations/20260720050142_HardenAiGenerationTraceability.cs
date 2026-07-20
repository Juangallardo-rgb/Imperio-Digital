using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimuladorApi.Migrations
{
    /// <inheritdoc />
    public partial class HardenAiGenerationTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsumedAt",
                table: "AiGenerationRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "AiGenerationRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MethodologyCode",
                table: "AiGenerationRecords",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseFormat",
                table: "AiGenerationRecords",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationRecords_RequestedByUserId_MethodologyCode_Opera~",
                table: "AiGenerationRecords",
                columns: new[] { "RequestedByUserId", "MethodologyCode", "OperationType", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiGenerationRecords_RequestedByUserId_MethodologyCode_Opera~",
                table: "AiGenerationRecords");

            migrationBuilder.DropColumn(
                name: "ConsumedAt",
                table: "AiGenerationRecords");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "AiGenerationRecords");

            migrationBuilder.DropColumn(
                name: "MethodologyCode",
                table: "AiGenerationRecords");

            migrationBuilder.DropColumn(
                name: "ResponseFormat",
                table: "AiGenerationRecords");
        }
    }
}
