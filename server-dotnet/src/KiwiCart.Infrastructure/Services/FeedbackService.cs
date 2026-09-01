using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KiwiCart.Infrastructure.Services;

public class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _db;

    public FeedbackService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FeedbackResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Feedback
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackResponse
            {
                Id = f.Id,
                UserName = f.UserName,
                Message = f.Message,
                Category = f.Category,
                CreatedAt = f.CreatedAt,
            })
            .ToListAsync(ct);
    }

    public async Task<FeedbackResponse> CreateAsync(
        string userId,
        string? userName,
        FeedbackRequest request,
        CancellationToken ct = default)
    {
        var feedback = new Feedback
        {
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "Anonymous" : userName,
            Message = request.Message.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "general" : request.Category.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Feedback.Add(feedback);
        await _db.SaveChangesAsync(ct);

        return new FeedbackResponse
        {
            Id = feedback.Id,
            UserName = feedback.UserName,
            Message = feedback.Message,
            Category = feedback.Category,
            CreatedAt = feedback.CreatedAt,
        };
    }
}
