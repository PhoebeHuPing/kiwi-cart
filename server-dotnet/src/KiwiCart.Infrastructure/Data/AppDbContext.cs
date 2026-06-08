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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            e.Property(p => p.Brand).HasColumnName("brand").HasMaxLength(200);
            e.Property(p => p.Category).HasColumnName("category").HasMaxLength(200);
        });

        modelBuilder.Entity<Store>(e =>
        {
            e.ToTable("stores");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(p => p.Brand).HasColumnName("brand").HasMaxLength(100).IsRequired();
            e.Property(p => p.Latitude).HasColumnName("latitude");
            e.Property(p => p.Longitude).HasColumnName("longitude");
            e.Property(p => p.Address).HasColumnName("address").HasMaxLength(500);
        });

        modelBuilder.Entity<Price>(e =>
        {
            e.ToTable("prices");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.ProductId).HasColumnName("product_id");
            e.Property(p => p.StoreId).HasColumnName("store_id");
            e.Property(p => p.Amount).HasColumnName("amount").HasPrecision(10, 2);
            e.Property(p => p.RetrievedAt).HasColumnName("retrieved_at");
            e.HasIndex(p => new { p.ProductId, p.StoreId }).IsUnique();
        });

        modelBuilder.Entity<Favorite>(e =>
        {
            e.ToTable("favorites");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.UserId).HasColumnName("user_id").HasMaxLength(200).IsRequired();
            e.Property(p => p.ProductId).HasColumnName("product_id");
            e.HasIndex(p => new { p.UserId, p.ProductId }).IsUnique();
        });

        modelBuilder.Entity<StoreToken>(e =>
        {
            e.ToTable("store_tokens");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.StoreBrand).HasColumnName("store_brand").HasMaxLength(100).IsRequired();
            e.Property(p => p.Token).HasColumnName("token").IsRequired();
            e.Property(p => p.IssuedAt).HasColumnName("issued_at");
            e.Property(p => p.ExpiresAt).HasColumnName("expires_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(p => p.StoreBrand).IsUnique();
            e.Ignore(p => p.IsExpired);
            e.Ignore(p => p.IsNearExpiry);
        });

        modelBuilder.Entity<Bucket>(e =>
        {
            e.ToTable("buckets");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.UserId).HasColumnName("user_id").HasMaxLength(200).IsRequired();
            e.Property(p => p.Name).HasColumnName("name").HasMaxLength(200);
            e.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.BucketId);
        });

        modelBuilder.Entity<BucketItem>(e =>
        {
            e.ToTable("bucket_items");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.BucketId).HasColumnName("bucket_id");
            e.Property(p => p.ProductId).HasColumnName("product_id");
            e.Property(p => p.Quantity).HasColumnName("quantity");
        });
    }
}
