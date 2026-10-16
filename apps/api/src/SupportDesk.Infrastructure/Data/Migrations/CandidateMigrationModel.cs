// Frozen EF model for the candidate migration series (not derived from live configurations).
// Re-generate with dotnet ef after making later changes to the model.
using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Data.Migrations;

internal static class CandidateMigrationModel
{
    public static void Build(ModelBuilder modelBuilder, bool withCredentials)
    {
        modelBuilder.Entity<SupportDesk.Domain.Aggregates.Agents.Agent>(builder =>
        {
        builder.ToTable("Agents", t =>
            t.HasCheckConstraint("CK_Agents_MaxOpenTickets", "[MaxOpenTickets] > 0"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName).HasMaxLength(200).IsRequired();

        builder.Property(a => a.Email).HasMaxLength(256).IsRequired();

        if (withCredentials) builder.Property(a => a.PasswordHash).HasMaxLength(256);
        else builder.Ignore(a => a.PasswordHash);

        builder.Property(a => a.CreatedAtUtc).HasColumnType("datetime2(3)");

        builder.HasIndex(a => a.Email).IsUnique().HasDatabaseName("UQ_Agents_Email");

        builder.HasMany(a => a.Specializations)
            .WithOne()
            .HasForeignKey(s => s.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Specializations).UsePropertyAccessMode(PropertyAccessMode.Field);
    
            builder.Property(x => x.CreatedAtUtc).HasConversion<UtcDateTimeConverter>();
        });

        modelBuilder.Entity<SupportDesk.Domain.Aggregates.Agents.AgentSpecialization>(builder =>
        {
        builder.ToTable("AgentSpecializations");

        builder.HasKey(s => new { s.AgentId, s.CategoryId });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.CategoryId)
            .HasDatabaseName("IX_AgentSpecializations_CategoryId")
            .IncludeProperties(s => s.AgentId);
    

        });

        modelBuilder.Entity<SupportDesk.Domain.Aggregates.Categories.Category>(builder =>
        {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("UQ_Categories_Name");
    

        });

        modelBuilder.Entity<SupportDesk.Domain.Aggregates.Customers.Customer>(builder =>
        {
        builder.ToTable("Customers", t =>
            t.HasCheckConstraint("CK_Customers_Tier", "[Tier] IN (1, 2)"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();

        builder.Property(c => c.Email).HasMaxLength(256).IsRequired();

        builder.Property(c => c.Phone).HasMaxLength(50);

        builder.Property(c => c.Tier).HasConversion<int>();

        builder.Property(c => c.CreatedAtUtc).HasColumnType("datetime2(3)");

        builder.HasIndex(c => c.Email).IsUnique().HasDatabaseName("UQ_Customers_Email");
    
            builder.Property(x => x.CreatedAtUtc).HasConversion<UtcDateTimeConverter>();
        });

        modelBuilder.Entity<SupportDesk.Domain.Aggregates.Tickets.Ticket>(builder =>
        {
        builder.ToTable("Tickets", t =>
        {
            t.HasCheckConstraint("CK_Tickets_Priority", "[Priority] BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_Tickets_Status", "[Status] BETWEEN 1 AND 5");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Reference).HasMaxLength(20).IsRequired();

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();

        builder.Property(t => t.Description).HasMaxLength(4000).IsRequired();

        builder.Property(t => t.Priority).HasConversion<int>();

        builder.Property(t => t.Status).HasConversion<int>();

        builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(t => t.UpdatedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(t => t.DueAtUtc).HasColumnType("datetime2(3)");

        builder.Property(t => t.SlaStartedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(t => t.ResolvedAtUtc).HasColumnType("datetime2(3)");

        // A ticket is never silently detached from the rows that give it meaning.
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Agent>()
            .WithMany()
            .HasForeignKey(t => t.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(t => t.IsOpen);

        builder.HasIndex(t => t.Reference).IsUnique().HasDatabaseName("UQ_Tickets_Reference");

        // Covers the list screen's most common filter pair.
        builder.HasIndex(t => new { t.Status, t.Priority })
            .HasDatabaseName("IX_Tickets_Status_Priority")
            .IncludeProperties(t => new { t.DueAtUtc, t.AssignedAgentId });

        builder.HasIndex(t => t.CustomerId).HasDatabaseName("IX_Tickets_CustomerId");

        builder.HasIndex(t => t.CategoryId).HasDatabaseName("IX_Tickets_CategoryId");

        builder.HasIndex(t => t.AssignedAgentId)
            .HasDatabaseName("IX_Tickets_AssignedAgentId")
            .HasFilter("[AssignedAgentId] IS NOT NULL");

        builder.HasIndex(t => t.DueAtUtc).HasDatabaseName("IX_Tickets_DueAtUtc");
    
            builder.Property(x => x.CreatedAtUtc).HasConversion<UtcDateTimeConverter>();
            builder.Property(x => x.UpdatedAtUtc).HasConversion<UtcDateTimeConverter>();
            builder.Property(x => x.DueAtUtc).HasConversion<NullableUtcDateTimeConverter>();
            builder.Property(x => x.SlaStartedAtUtc).HasConversion<NullableUtcDateTimeConverter>();
            builder.Property(x => x.ResolvedAtUtc).HasConversion<NullableUtcDateTimeConverter>();
        });

        modelBuilder.Entity<SupportDesk.Domain.Aggregates.Tickets.TicketEscalation>(b =>
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
    
            b.Property(x => x.FromDueAtUtc).HasConversion<NullableUtcDateTimeConverter>();
            b.Property(x => x.ToDueAtUtc).HasConversion<NullableUtcDateTimeConverter>();
            b.Property(x => x.EscalatedAtUtc).HasConversion<UtcDateTimeConverter>();
        });
    }
}
