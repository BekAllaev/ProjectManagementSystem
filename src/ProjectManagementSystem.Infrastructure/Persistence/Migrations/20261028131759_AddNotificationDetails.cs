using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                schema: "dbo",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                schema: "dbo",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "dbo",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkPackageId",
                schema: "dbo",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientEmployeeId",
                schema: "dbo",
                table: "Notifications",
                column: "RecipientEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientEmployeeId",
                schema: "dbo",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "dbo",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TaskId",
                schema: "dbo",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "dbo",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "WorkPackageId",
                schema: "dbo",
                table: "Notifications");
        }
    }
}
