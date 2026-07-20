using Xunit;

namespace StrongKingJames.Data.Tests;

[Collection("db")]
public class FixtureSmokeTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Migrations_apply_and_context_connects()
    {
        await using var ctx = fx.CreateContext();
        Assert.True(await ctx.Database.CanConnectAsync());
    }
}
