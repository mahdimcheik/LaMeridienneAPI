using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LaMeridienneAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Newspapers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_NormalizedName",
                table: "Customers");

            migrationBuilder.AddColumn<Guid>(
                name: "MergedIntoId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Newspapers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Newspapers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewspaperSales",
                columns: table => new
                {
                    SheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewspaperId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewspaperSales", x => new { x.SheetId, x.NewspaperId });
                    table.CheckConstraint("CK_NewspaperSale_Quantity", "\"Quantity\" >= 0 AND \"Quantity\" <= 999");
                    table.ForeignKey(
                        name: "FK_NewspaperSales_DailySheets_SheetId",
                        column: x => x.SheetId,
                        principalTable: "DailySheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewspaperSales_Newspapers_NewspaperId",
                        column: x => x.NewspaperId,
                        principalTable: "Newspapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Newspapers",
                columns: new[] { "Id", "IsActive", "Name", "NormalizedName", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("01920000-0000-7000-8000-000000000001"), true, "Sud Ouest", "sud ouest", 1 },
                    { new Guid("01920000-0000-7000-8000-000000000002"), true, "Le Républicain", "le republicain", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_MergedIntoId",
                table: "Customers",
                column: "MergedIntoId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedName",
                table: "Customers",
                column: "NormalizedName",
                unique: true,
                filter: "\"MergedIntoId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Newspapers_NormalizedName",
                table: "Newspapers",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewspaperSales_NewspaperId",
                table: "NewspaperSales",
                column: "NewspaperId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Customers_MergedIntoId",
                table: "Customers",
                column: "MergedIntoId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Customers_MergedIntoId",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "NewspaperSales");

            migrationBuilder.DropTable(
                name: "Newspapers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_MergedIntoId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_NormalizedName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MergedIntoId",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedName",
                table: "Customers",
                column: "NormalizedName",
                unique: true);
        }
    }
}
