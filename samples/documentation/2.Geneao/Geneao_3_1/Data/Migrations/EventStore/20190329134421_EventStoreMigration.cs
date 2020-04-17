using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Geneao.Data.Migrations.EventStore
{
    public partial class EventStoreMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SerializedAggregateId = table.Column<string>(nullable: true),
                    HashedAggregateId = table.Column<int>(nullable: true),
                    AggregateIdType = table.Column<string>(nullable: true),
                    AggregateType = table.Column<string>(maxLength: 1024, nullable: true),
                    EventData = table.Column<string>(nullable: false),
                    EventType = table.Column<string>(maxLength: 1024, nullable: false),
                    EventTime = table.Column<DateTime>(nullable: false),
                    Sequence = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Snapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    HashedAggregateId = table.Column<int>(nullable: false),
                    AggregateType = table.Column<string>(maxLength: 1024, nullable: true),
                    SnapshotData = table.Column<string>(nullable: false),
                    SnapshotTime = table.Column<DateTime>(nullable: false),
                    SnapshotBehaviorType = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventType",
                table: "Event",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_Event_HashedAggregateId_AggregateType",
                table: "Event",
                columns: new[] { "HashedAggregateId", "AggregateType" });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_HashedAggregateId_AggregateType",
                table: "Snapshot",
                columns: new[] { "HashedAggregateId", "AggregateType" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "Snapshot");
        }
    }
}
