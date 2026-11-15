using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTDS.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2y$11$LAsjsD6AhrIhEfhMWwzqWeGqvZ0NcW6JskpB/P3EF./zy/FuGp3sS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2a$11$E9P.h40f51W37E2U22C.7uC4.vFv2.R/f4PjC2j78K1hG8E/w0C7a");
        }
    }
}
