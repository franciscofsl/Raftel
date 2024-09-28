using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raftel.Data.Tests.Migrations
{
    /// <inheritdoc />
    public partial class AddProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntegerValue",
                table: "SampleModels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StringValue",
                table: "SampleModels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SampleNotAuditedAggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleNotAuditedAggregates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SampleNotAuditedAggregates");

            migrationBuilder.DropColumn(
                name: "IntegerValue",
                table: "SampleModels");

            migrationBuilder.DropColumn(
                name: "StringValue",
                table: "SampleModels");
        }
    }
}
