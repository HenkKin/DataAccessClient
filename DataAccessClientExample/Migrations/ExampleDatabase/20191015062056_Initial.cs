using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessClientExample.Migrations.ExampleDatabase
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExampleEntities",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedOn = table.Column<DateTime>(nullable: false),
                    CreatedById = table.Column<int>(nullable: false),
                    ModifiedOn = table.Column<DateTime>(nullable: true),
                    ModifiedById = table.Column<int>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    DeletedOn = table.Column<DateTime>(nullable: true),
                    DeletedById = table.Column<int>(nullable: true),
                    RowVersion = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExampleEntityTranslation",
                columns: table => new
                {
                    Language = table.Column<string>(nullable: false),
                    TranslatedEntityId = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleEntityTranslation", x => new { x.TranslatedEntityId, x.Language });
                    table.ForeignKey(
                        name: "FK_ExampleEntityTranslation_ExampleEntities_TranslatedEntityId",
                        column: x => x.TranslatedEntityId,
                        principalTable: "ExampleEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExampleEntityTranslation");

            migrationBuilder.DropTable(
                name: "ExampleEntities");
        }
    }
}
