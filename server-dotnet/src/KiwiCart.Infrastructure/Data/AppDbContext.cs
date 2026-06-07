using KiwiCart.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace KiwiCart.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Price> Prices => Set<Price>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<StoreToken> StoreTokens => Set<StoreToken>();
    public DbSet<Bucket> Buckets => Set<Bucket>();
    public DbSet<BucketItem> BucketItems => Set<BucketItem>();
}
