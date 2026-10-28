using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReadModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "read");

            migrationBuilder.CreateTable(
                name: "DependencyItems",
                schema: "read",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessorTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuccessorTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessorWorkPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PredecessorWorkPackageDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PredecessorStatus = table.Column<int>(type: "int", nullable: false),
                    SuccessorWorkPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuccessorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SuccessorWorkPackageDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SuccessorStatus = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsAcrossWorkPackages = table.Column<bool>(type: "bit", nullable: false),
                    HasWarning = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyItems", x => new { x.ProjectId, x.PredecessorTaskId, x.SuccessorTaskId });
                });

            migrationBuilder.CreateTable(
                name: "FundingTrancheItems",
                schema: "read",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingTrancheItems", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "HourlyRateItems",
                schema: "read",
                columns: table => new
                {
                    TeamMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyRateItems", x => new { x.TeamMemberId, x.From });
                });

            migrationBuilder.CreateTable(
                name: "MemberCostItems",
                schema: "read",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberCostItems", x => new { x.ProjectId, x.EmployeeId });
                });

            migrationBuilder.CreateTable(
                name: "MembershipPeriods",
                schema: "read",
                columns: table => new
                {
                    TeamMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPeriods", x => x.TeamMemberId);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneItems",
                schema: "read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedTasks = table.Column<int>(type: "int", nullable: false),
                    CountedTasks = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneTaskItems",
                schema: "read",
                columns: table => new
                {
                    MilestoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WorkPackageDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsCounted = table.Column<bool>(type: "bit", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneTaskItems", x => new { x.MilestoneId, x.TaskId });
                });

            migrationBuilder.CreateTable(
                name: "ProjectItems",
                schema: "read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenTasks = table.Column<int>(type: "int", nullable: false),
                    InProgressTasks = table.Column<int>(type: "int", nullable: false),
                    CompletedTasks = table.Column<int>(type: "int", nullable: false),
                    CancelledTasks = table.Column<int>(type: "int", nullable: false),
                    CountedTasks = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    TotalFunding = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingFunding = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UsedPercent = table.Column<decimal>(type: "decimal(9,1)", precision: 9, scale: 1, nullable: true),
                    IsOverspent = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    ScheduleVersion = table.Column<long>(type: "bigint", nullable: false),
                    FundingVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleItems",
                schema: "read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsAssigned = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                schema: "read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WorkPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkPackageDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    WorkPackageResponsiblePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResponsiblePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EstimatedEffort = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    LoggedHours = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    LoggedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HasUnfinishedFinishToStartPredecessor = table.Column<bool>(type: "bit", nullable: false),
                    WorkPackageVersion = table.Column<long>(type: "bigint", nullable: false),
                    TimeLogVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamMemberItems",
                schema: "read",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMemberItems", x => new { x.ProjectId, x.EmployeeId });
                });

            migrationBuilder.CreateTable(
                name: "TimeEntryItems",
                schema: "read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkPackageCostItems",
                schema: "read",
                columns: table => new
                {
                    WorkPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPackageCostItems", x => x.WorkPackageId);
                });

            migrationBuilder.CreateTable(
                name: "WorkPackageItems",
                schema: "read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ResponsiblePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRegistered = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedTasks = table.Column<int>(type: "int", nullable: false),
                    CountedTasks = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPackageItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundingTrancheItems_ProjectId",
                schema: "read",
                table: "FundingTrancheItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyRateItems_ProjectId",
                schema: "read",
                table: "HourlyRateItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPeriods_EmployeeId",
                schema: "read",
                table: "MembershipPeriods",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPeriods_ProjectId_EmployeeId",
                schema: "read",
                table: "MembershipPeriods",
                columns: new[] { "ProjectId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneItems_ProjectId",
                schema: "read",
                table: "MilestoneItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneTaskItems_ProjectId",
                schema: "read",
                table: "MilestoneTaskItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleItems_ProjectId",
                schema: "read",
                table: "RoleItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId",
                schema: "read",
                table: "TaskItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ResponsiblePersonId",
                schema: "read",
                table: "TaskItems",
                column: "ResponsiblePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberItems_EmployeeId",
                schema: "read",
                table: "TeamMemberItems",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntryItems_ProjectId",
                schema: "read",
                table: "TimeEntryItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntryItems_TaskId",
                schema: "read",
                table: "TimeEntryItems",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPackageCostItems_ProjectId",
                schema: "read",
                table: "WorkPackageCostItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPackageItems_ProjectId",
                schema: "read",
                table: "WorkPackageItems",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DependencyItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "FundingTrancheItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "HourlyRateItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "MemberCostItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "MembershipPeriods",
                schema: "read");

            migrationBuilder.DropTable(
                name: "MilestoneItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "MilestoneTaskItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "ProjectItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "RoleItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "TaskItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "TeamMemberItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "TimeEntryItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "WorkPackageCostItems",
                schema: "read");

            migrationBuilder.DropTable(
                name: "WorkPackageItems",
                schema: "read");
        }
    }
}
