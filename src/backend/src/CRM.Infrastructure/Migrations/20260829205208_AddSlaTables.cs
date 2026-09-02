using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkDays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstResponseMinutes = table.Column<int>(type: "int", nullable: false),
                    ResolutionMinutes = table.Column<int>(type: "int", nullable: false),
                    WarningThresholdPercent = table.Column<int>(type: "int", nullable: false),
                    BreachThresholdPercent = table.Column<int>(type: "int", nullable: false),
                    CriticalBreachThresholdPercent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketSlas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlaPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockStartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClockPausedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccumulatedPauseMinutes = table.Column<int>(type: "int", nullable: false),
                    FirstResponseDue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionDue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstResponseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstResponseBreached = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionBreached = table.Column<bool>(type: "bit", nullable: false),
                    BreachTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketSlas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessHourHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessHoursId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHourHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessHourHolidays_BusinessHours_BusinessHoursId",
                        column: x => x.BusinessHoursId,
                        principalTable: "BusinessHours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHourHolidays_BusinessHoursId",
                table: "BusinessHourHolidays",
                column: "BusinessHoursId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlas_TicketId",
                table: "TicketSlas",
                column: "TicketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessHourHolidays");

            migrationBuilder.DropTable(
                name: "SlaPolicies");

            migrationBuilder.DropTable(
                name: "TicketSlas");

            migrationBuilder.DropTable(
                name: "BusinessHours");
        }
    }
}
