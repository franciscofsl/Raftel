using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raftel.Demo.Data.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TranslationResources_Languages_LanguageId",
                table: "TranslationResources");

            migrationBuilder.AlterColumn<Guid>(
                name: "LanguageId",
                table: "TranslationResources",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TranslationResources_Languages_LanguageId",
                table: "TranslationResources",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TranslationResources_Languages_LanguageId",
                table: "TranslationResources");

            migrationBuilder.AlterColumn<Guid>(
                name: "LanguageId",
                table: "TranslationResources",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_TranslationResources_Languages_LanguageId",
                table: "TranslationResources",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id");
        }
    }
}
