using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KiwiCart.Tests.Services;

public class FeedbackServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("FeedbackServiceTest_" + Guid.NewGuid())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_PersistsFeedbackAndReturnsResponse()
    {
        using var db = CreateDb();
        var sut = new FeedbackService(db);

        var response = await sut.CreateAsync(
            "auth0|user123", "Phoebe",
            new FeedbackRequest { Message = "Great app!", Category = "praise" });

        Assert.NotEqual(0, response.Id);
        Assert.Equal("Phoebe", response.UserName);
        Assert.Equal("Great app!", response.Message);
        Assert.Equal("praise", response.Category);

        // Persisted with the sensitive user_id in the DB (but not exposed via DTO).
        var stored = await db.Feedback.SingleAsync();
        Assert.Equal("auth0|user123", stored.UserId);
    }

    [Fact]
    public async Task CreateAsync_TrimsMessage()
    {
        using var db = CreateDb();
        var sut = new FeedbackService(db);

        var response = await sut.CreateAsync(
            "u1", "Bob", new FeedbackRequest { Message = "  spaced  " });

        Assert.Equal("spaced", response.Message);
    }

    [Fact]
    public async Task CreateAsync_DefaultsCategoryToGeneral_WhenNullOrBlank()
    {
        using var db = CreateDb();
        var sut = new FeedbackService(db);

        var r1 = await sut.CreateAsync("u1", "A", new FeedbackRequest { Message = "m", Category = null });
        var r2 = await sut.CreateAsync("u2", "B", new FeedbackRequest { Message = "m", Category = "   " });

        Assert.Equal("general", r1.Category);
        Assert.Equal("general", r2.Category);
    }

    [Fact]
    public async Task CreateAsync_DefaultsUserNameToAnonymous_WhenNullOrBlank()
    {
        using var db = CreateDb();
        var sut = new FeedbackService(db);

        var response = await sut.CreateAsync("u1", null, new FeedbackRequest { Message = "m" });

        Assert.Equal("Anonymous", response.UserName);
    }

    [Fact]
    public async Task GetAllAsync_ProjectionExcludesUserId()
    {
        using var db = CreateDb();
        db.Feedback.Add(new Feedback
        {
            UserId = "auth0|SECRET", UserName = "Phoebe",
            Message = "hi", Category = "general", CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        var sut = new FeedbackService(db);

        var results = await sut.GetAllAsync();

        var item = Assert.Single(results);
        // FeedbackResponse has no UserId property at all — verified at compile time.
        // Assert the public fields are present and correct.
        Assert.Equal("Phoebe", item.UserName);
        Assert.Equal("hi", item.Message);
        Assert.Equal("general", item.Category);
        // Ensure no public property carries the secret user id value.
        var propertyValues = typeof(FeedbackResponse)
            .GetProperties()
            .Select(p => p.GetValue(item)?.ToString());
        Assert.DoesNotContain("auth0|SECRET", propertyValues);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        using var db = CreateDb();
        var now = DateTime.UtcNow;
        db.Feedback.AddRange(
            new Feedback { UserId = "u1", UserName = "Old", Message = "old", Category = "general", CreatedAt = now.AddHours(-2) },
            new Feedback { UserId = "u2", UserName = "New", Message = "new", Category = "general", CreatedAt = now },
            new Feedback { UserId = "u3", UserName = "Mid", Message = "mid", Category = "general", CreatedAt = now.AddHours(-1) });
        await db.SaveChangesAsync();
        var sut = new FeedbackService(db);

        var results = await sut.GetAllAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal("New", results[0].UserName);
        Assert.Equal("Mid", results[1].UserName);
        Assert.Equal("Old", results[2].UserName);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoFeedback()
    {
        using var db = CreateDb();
        var sut = new FeedbackService(db);

        var results = await sut.GetAllAsync();

        Assert.Empty(results);
    }
}
