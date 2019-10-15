using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessClientExample.Migrations.ExampleSecondDatabase
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExampleSecondEntities",
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
                    table.PrimaryKey("PK_ExampleSecondEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExampleSecondEntity_CodeTranslations",
                columns: table => new
                {
                    OwnerId = table.Column<int>(nullable: false),
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Translation = table.Column<string>(nullable: false),
                    Language = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleSecondEntity_CodeTranslations", x => new { x.OwnerId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExampleSecondEntity_CodeTranslations_ExampleSecondEntities_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "ExampleSecondEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExampleSecondEntity_DescriptionTranslations",
                columns: table => new
                {
                    OwnerId = table.Column<int>(nullable: false),
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Translation = table.Column<string>(nullable: false),
                    Language = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleSecondEntity_DescriptionTranslations", x => new { x.OwnerId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExampleSecondEntity_DescriptionTranslations_ExampleSecondEntities_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "ExampleSecondEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExampleSecondEntity_CodeTranslations");

            migrationBuilder.DropTable(
                name: "ExampleSecondEntity_DescriptionTranslations");

            migrationBuilder.DropTable(
                name: "ExampleSecondEntities");
        }
    }
}
