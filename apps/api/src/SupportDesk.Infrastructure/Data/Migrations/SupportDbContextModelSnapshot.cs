using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SupportDesk.Infrastructure.Data.Migrations;

[DbContext(typeof(SupportDbContext))]
partial class SupportDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) =>
        CandidateMigrationModel.Build(modelBuilder, withCredentials: true);
}
