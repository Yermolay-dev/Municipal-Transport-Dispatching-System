using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MTDS.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Routes",
                columns: new[] { "Id", "PathNumber", "RouteLineJson", "TransportType" },
                values: new object[] { 1, "49a", "[[55.4311, 37.5458], [55.4280, 37.5490], [55.4200, 37.5500]]", "bus" });

            migrationBuilder.InsertData(
                table: "Stops",
                columns: new[] { "Id", "Lat", "Lon", "Name" },
                values: new object[,]
                {
                    { 1, 55.431100000000001, 37.5458, "Автостанция Подольск" },
                    { 2, 55.427999999999997, 37.548999999999999, "Площадь Ленина" },
                    { 3, 55.420000000000002, 37.549999999999997, "Рынок" }
                });

            migrationBuilder.InsertData(
                table: "Transports",
                columns: new[] { "Id", "Capacity", "NumberPlate", "Status", "Type" },
                values: new object[] { 105, 50, "A154AA150", "in_garage", "bus" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DriverLicenseNumber", "Email", "LastEntranceAt", "Name", "PasswordHash", "PhoneNumber", "PhotoUrl", "Role", "SurName" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "", "admin@mtds.ru", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ермолай", "$2a$11$E9P.h40f51W37E2U22C.7uC4.vFv2.R/f4PjC2j78K1hG8E/w0C7a", "", "", "Dispatcher", "Лукинов" });

            migrationBuilder.InsertData(
                table: "RouteStops",
                columns: new[] { "Id", "MinutesFromStart", "RouteId", "StopId", "StopOrder" },
                values: new object[,]
                {
                    { 1, 0, 1, 1, 1 },
                    { 2, 5, 1, 2, 2 },
                    { 3, 17, 1, 3, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RouteStops",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RouteStops",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RouteStops",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Transports",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Routes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Stops",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Stops",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Stops",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
