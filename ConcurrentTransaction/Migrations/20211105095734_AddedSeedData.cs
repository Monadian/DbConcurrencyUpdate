using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConcurentTransaction.Migrations
{
    public partial class AddedSeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Wallets",
                columns: new[] { "Id", "Amount", "Balance", "UserId" },
                values: new object[] { new Guid("6816bd4e-296f-414d-a196-2833d1a980d7"), 0m, 0m, new Guid("05fbaad2-994a-4f5f-9ff6-ad5023514fb3") });

            migrationBuilder.InsertData(
                table: "Wallets",
                columns: new[] { "Id", "Amount", "Balance", "UserId" },
                values: new object[] { new Guid("fb3d9286-c971-4a6e-9028-59493ff143fc"), 0m, 0m, new Guid("8a36dd49-e7af-4957-8707-dad7e06e8dc7") });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Wallets",
                keyColumn: "Id",
                keyValue: new Guid("6816bd4e-296f-414d-a196-2833d1a980d7"));

            migrationBuilder.DeleteData(
                table: "Wallets",
                keyColumn: "Id",
                keyValue: new Guid("fb3d9286-c971-4a6e-9028-59493ff143fc"));
        }
    }
}
