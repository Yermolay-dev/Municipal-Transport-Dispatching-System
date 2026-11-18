using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTDS.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Action",
                table: "AuditLogs",
                newName: "Reason");

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2a$11$LAsjsD6AhrIhEfhMWwzqWeGqvZ0NcW6JskpB/P3EF./zy/FuGp3sS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "AuditLogs",
                newName: "Action");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2y$11$LAsjsD6AhrIhEfhMWwzqWeGqvZ0NcW6JskpB/P3EF./zy/FuGp3sS");
        }
    }
}
