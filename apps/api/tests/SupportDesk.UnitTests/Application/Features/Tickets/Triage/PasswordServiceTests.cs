using SupportDesk.Infrastructure.Services;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Triage;

public class PasswordServiceTests
{
    [Fact]
    public void Pbkdf2HashIncludesUniqueSaltAndVerifiesOnlyCorrectPassword()
    {
        var first = PasswordService.Hash("DevOnly!Support2026");
        var second = PasswordService.Hash("DevOnly!Support2026");
        Assert.NotEqual(first, second);
        Assert.True(PasswordService.Verify("DevOnly!Support2026", first));
        Assert.False(PasswordService.Verify("wrong", first));
        Assert.False(PasswordService.Verify("anything", null));
    }
}
