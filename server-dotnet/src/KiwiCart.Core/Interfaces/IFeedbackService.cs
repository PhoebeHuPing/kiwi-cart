using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IFeedbackService
{
    Task<IReadOnlyList<FeedbackResponse>> GetAllAsync(CancellationToken ct = default);

    Task<FeedbackResponse> CreateAsync(
        string userId,
        string? userName,
        FeedbackRequest request,
        CancellationToken ct = default);
}
