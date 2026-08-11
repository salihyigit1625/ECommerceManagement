using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceManagement.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUsersFromSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "PasswordHash", "RefreshToken", "RefreshTokenExpiresAt", "Username" },
                values: new object[] { 1, new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Utc), "superadmin@sistem.com", true, "SuperAdmin123!", null, null, "superadmin" });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 1, 1 });
        }
    }
}
