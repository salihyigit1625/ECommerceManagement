using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceManagement.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SyncWarehouseSysmondId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SysmondId",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SysmondStockId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SysmondId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SysmondStockId",
                table: "Products");
        }
    }
}
