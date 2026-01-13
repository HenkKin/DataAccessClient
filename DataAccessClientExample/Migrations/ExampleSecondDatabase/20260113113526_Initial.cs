using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessClientExample.Migrations.ExampleSecondDatabase
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "ExampleSecondEntities",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedById = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedById = table.Column<int>(type: "integer", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleSecondEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExampleSecondEntity_CodeTranslations",
                schema: "dbo",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Translation = table.Column<string>(type: "text", nullable: false),
                    LocaleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleSecondEntity_CodeTranslations", x => new { x.OwnerId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExampleSecondEntity_CodeTranslations_ExampleSecondEntities_~",
                        column: x => x.OwnerId,
                        principalSchema: "dbo",
                        principalTable: "ExampleSecondEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExampleSecondEntity_DescriptionTranslations",
                schema: "dbo",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Translation = table.Column<string>(type: "text", nullable: false),
                    LocaleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExampleSecondEntity_DescriptionTranslations", x => new { x.OwnerId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExampleSecondEntity_DescriptionTranslations_ExampleSecondEn~",
                        column: x => x.OwnerId,
                        principalSchema: "dbo",
                        principalTable: "ExampleSecondEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExampleSecondEntity_CodeTranslations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ExampleSecondEntity_DescriptionTranslations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ExampleSecondEntities",
                schema: "dbo");
        }
    }
}
