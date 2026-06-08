using KiwiCart.Core.DTOs;

namespace KiwiCart.Core.Interfaces;

public interface IBucketService
{
    Task<IReadOnlyList<BucketCompareResult>> CompareAsync(
        List<BucketItemInput> items, CancellationToken ct = default);
}
