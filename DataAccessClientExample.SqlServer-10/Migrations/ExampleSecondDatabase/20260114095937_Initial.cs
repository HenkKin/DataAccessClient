using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessClientExample.SqlServer.Migrations.ExampleSecondDatabase
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExampleSecondEntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleSecondEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExampleSecondEntity_CodeTranslations",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Translation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocaleId = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Translation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocaleId = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

        /// <inheritdoc />
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
