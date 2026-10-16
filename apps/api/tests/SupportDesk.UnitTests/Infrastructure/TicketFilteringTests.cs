using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure;

public class TicketFilteringTests
{
    [Fact]
    public async Task PriorityIsExactAndCountIsFilteredBeforePaging()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Queries.GetPagedAsync(new TicketQuery { Priority = TicketPriority.High, PageSize = 1 }, default);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.All(result.Items, item => Assert.Equal(TicketPriority.High, item.Priority));
    }

    [Fact]
    public async Task FiltersCombineWithAnd()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Queries.GetPagedAsync(new TicketQuery {
            Priority = TicketPriority.High, Status = TicketStatus.Open, CustomerId = fixture.FirstCustomerId
        }, default);
        Assert.Single(result.Items);
        Assert.Equal("TCK-0001", result.Items[0].Reference);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task CustomerNameSearchIsTrimmedAndCombinedWithPriority()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Queries.GetPagedAsync(new TicketQuery {
            Search = "  Contoso  ", Priority = TicketPriority.High
        }, default);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("Contoso Ltd", item.Customer.Name));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly SupportDbContext db;
        public TicketQueries Queries { get; }
        public int FirstCustomerId { get; private set; }

        private Fixture(SqliteConnection connection, SupportDbContext db)
        {
            this.connection = connection;
            this.db = db;
            Queries = new TicketQueries(db, new FixedClock(), Options.Create(new SlaOptions()));
        }

        public static async Task<Fixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new SupportDbContext(new DbContextOptionsBuilder<SupportDbContext>()
                .UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var fixture = new Fixture(connection, db);
            var now = FixedClock.DefaultNow;
            var contoso = new Customer("Contoso Ltd", "contoso@example.test", null, CustomerTier.Premium, now);
            var other = new Customer("Fabrikam Inc", "fabrikam@example.test", null, CustomerTier.Standard, now);
            var category = new Category("Technical", false, false);
            db.AddRange(contoso, other, category);
            await db.SaveChangesAsync();
            fixture.FirstCustomerId = contoso.Id;
            var t1 = Ticket.Raise("TCK-0001", "Network unavailable", "Network is down", contoso.Id, category.Id, TicketPriority.High, now);
            t1.ChangeStatus(TicketStatus.Open, now);
            var t2 = Ticket.Raise("TCK-0002", "Export issue", "Export is missing", contoso.Id, category.Id, TicketPriority.High, now);
            var t3 = Ticket.Raise("TCK-0003", "Incorrect invoice", "Invoice is wrong", other.Id, category.Id, TicketPriority.Low, now);
            db.AddRange(t1, t2, t3);
            await db.SaveChangesAsync();
            return fixture;
        }
        public async ValueTask DisposeAsync()
        {
            await db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
