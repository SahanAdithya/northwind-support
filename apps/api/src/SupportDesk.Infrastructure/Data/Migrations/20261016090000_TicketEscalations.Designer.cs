using Microsoft.EntityFrameworkCore;
namespace SupportDesk.Infrastructure.Data.Migrations;
public partial class TicketEscalations
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        CandidateMigrationModel.Build(modelBuilder, withCredentials: false);
}
