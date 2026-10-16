using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportDesk.Infrastructure.Data;

#nullable disable

namespace SupportDesk.Infrastructure.Data.Migrations;

[DbContext(typeof(SupportDbContext))]
[Migration("20261016091500_AgentCredentials")]
public partial class AgentCredentials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "PasswordHash", table: "Agents",
            type: "nvarchar(256)", maxLength: 256, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "PasswordHash", table: "Agents");
}
