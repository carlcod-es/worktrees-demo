using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uShopDemo.Web.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ushopCart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ushopCart", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ushopCartLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ushopCartLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ushopCartLine_ushopCart_CartId",
                        column: x => x.CartId,
                        principalTable: "ushopCart",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ushopCart_OrderNumber",
                table: "ushopCart",
                column: "OrderNumber",
                unique: true,
                filter: "[OrderNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ushopCartLine_CartId",
                table: "ushopCartLine",
                column: "CartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ushopCartLine");

            migrationBuilder.DropTable(
                name: "ushopCart");
        }
    }
}
