using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raftel.Demo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pirates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bounty = table.Column<long>(type: "bigint", nullable: false),
                    IsKing = table.Column<bool>(type: "bit", nullable: false),
                    BodyType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pirates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DevilFruits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PirateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevilFruits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevilFruits_Pirates_PirateId",
                        column: x => x.PirateId,
                        principalTable: "Pirates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EatenDevilFruitsByPirates",
                columns: table => new
                {
                    DevilFruitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PirateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EatenDevilFruitsByPirates", x => new { x.DevilFruitId, x.PirateId });
                    table.ForeignKey(
                        name: "FK_EatenDevilFruitsByPirates_DevilFruits_DevilFruitId",
                        column: x => x.DevilFruitId,
                        principalTable: "DevilFruits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EatenDevilFruitsByPirates_Pirates_PirateId",
                        column: x => x.PirateId,
                        principalTable: "Pirates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevilFruits_PirateId",
                table: "DevilFruits",
                column: "PirateId");

            migrationBuilder.CreateIndex(
                name: "IX_EatenDevilFruitsByPirates_PirateId",
                table: "EatenDevilFruitsByPirates",
                column: "PirateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EatenDevilFruitsByPirates");

            migrationBuilder.DropTable(
                name: "Ships");

            migrationBuilder.DropTable(
                name: "DevilFruits");

            migrationBuilder.DropTable(
                name: "Pirates");
        }
    }
}
