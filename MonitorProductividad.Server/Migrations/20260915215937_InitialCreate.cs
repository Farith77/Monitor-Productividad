using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using MonitorProductividad.Server.Entities;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MonitorProductividad.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Workstations",
                columns: table => new
                {
                    MacAddress = table.Column<string>(type: "text", nullable: false),
                    Hostname = table.Column<string>(type: "text", nullable: false),
                    RolAsignado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workstations", x => x.MacAddress);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    WindowTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CpuPercentage = table.Column<double>(type: "double precision", nullable: false),
                    RamUsageMb = table.Column<int>(type: "integer", nullable: false),
                    IdleSeconds = table.Column<double>(type: "double precision", nullable: false),
                    BackgroundWindows = table.Column<List<BackgroundWindow>>(type: "jsonb", nullable: false),
                    WorkstationId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetryRecords_Workstations_WorkstationId",
                        column: x => x.WorkstationId,
                        principalTable: "Workstations",
                        principalColumn: "MacAddress",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_WorkstationId",
                table: "TelemetryRecords",
                column: "WorkstationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelemetryRecords");

            migrationBuilder.DropTable(
                name: "Workstations");
        }
    }
}
