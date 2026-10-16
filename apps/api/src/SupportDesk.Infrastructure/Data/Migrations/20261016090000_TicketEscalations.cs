using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportDesk.Infrastructure.Data;

#nullable disable

namespace SupportDesk.Infrastructure.Data.Migrations;

[DbContext(typeof(SupportDbContext))]
[Migration("20261016090000_TicketEscalations")]
public partial class TicketEscalations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "SlaStartedAtUtc", table: "Tickets",
            type: "datetime2(3)", nullable: true);
        migrationBuilder.CreateTable(
            name: "TicketEscalations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TicketId = table.Column<int>(type: "int", nullable: false),
                FromPriority = table.Column<int>(type: "int", nullable: false),
                ToPriority = table.Column<int>(type: "int", nullable: false),
                FromAgentId = table.Column<int>(type: "int", nullable: true),
                ToAgentId = table.Column<int>(type: "int", nullable: true),
                FromDueAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ToDueAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                EscalatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EscalatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TicketEscalations", x => x.Id);
                table.ForeignKey("FK_TicketEscalations_Tickets_TicketId", x => x.TicketId,
                    "Tickets", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TicketEscalations_Agents_FromAgentId", x => x.FromAgentId,
                    "Agents", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TicketEscalations_Agents_ToAgentId", x => x.ToAgentId,
                    "Agents", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_TicketEscalations_TicketId_EscalatedAtUtc",
            "TicketEscalations", new[] { "TicketId", "EscalatedAtUtc" });
        migrationBuilder.CreateIndex("IX_TicketEscalations_FromAgentId", "TicketEscalations", "FromAgentId");
        migrationBuilder.CreateIndex("IX_TicketEscalations_ToAgentId", "TicketEscalations", "ToAgentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TicketEscalations");
        migrationBuilder.DropColumn(name: "SlaStartedAtUtc", table: "Tickets");
    }
}
