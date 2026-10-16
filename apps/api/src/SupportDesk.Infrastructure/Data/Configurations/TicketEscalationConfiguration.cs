using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Infrastructure.Data.Configurations;

public sealed class TicketEscalationConfiguration : IEntityTypeConfiguration<TicketEscalation>
{
    public void Configure(EntityTypeBuilder<TicketEscalation> b)
    {
        b.ToTable("TicketEscalations");
        b.HasKey(x => x.Id);
        b.Property(x => x.FromPriority).HasConversion<int>();
        b.Property(x => x.ToPriority).HasConversion<int>();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.EscalatedBy).HasMaxLength(100).IsRequired();
        b.Property(x => x.FromDueAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.ToDueAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.EscalatedAtUtc).HasColumnType("datetime2(3)");
        b.HasOne<Ticket>().WithMany().HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupportDesk.Domain.Aggregates.Agents.Agent>().WithMany()
            .HasForeignKey(x => x.FromAgentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupportDesk.Domain.Aggregates.Agents.Agent>().WithMany()
            .HasForeignKey(x => x.ToAgentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TicketId, x.EscalatedAtUtc })
            .HasDatabaseName("IX_TicketEscalations_TicketId_EscalatedAtUtc");
    }
}
