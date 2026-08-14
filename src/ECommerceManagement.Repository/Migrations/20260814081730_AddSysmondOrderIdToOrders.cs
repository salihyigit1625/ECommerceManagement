using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceManagement.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSysmondOrderIdToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SysmondOrderId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SysmondOrderItemId",
                table: "OrderItems",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SysmondOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SysmondOrderItemId",
                table: "OrderItems");
        }
    }
}
