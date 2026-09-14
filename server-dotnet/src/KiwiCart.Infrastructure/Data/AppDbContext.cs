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
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<ProductGtin> ProductGtins => Set<ProductGtin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            e.Property(p => p.Brand).HasColumnName("brand").HasMaxLength(200);
            e.Property(p => p.Category).HasColumnName("category").HasMaxLength(200);
            e.Property(p => p.ImageUrl).HasColumnName("image_url").HasMaxLength(1000);
            e.Property(p => p.ExternalProductId).HasColumnName("external_product_id").HasMaxLength(100);
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
            e.Property(p => p.ExternalStoreId).HasColumnName("external_store_id").HasMaxLength(100);
            // A retailer's store id is unique within that brand.
            e.HasIndex(p => new { p.Brand, p.ExternalStoreId }).IsUnique();
        });

        modelBuilder.Entity<Price>(e =>
        {
            e.ToTable("prices");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.ProductId).HasColumnName("product_id");
            e.Property(p => p.StoreId).HasColumnName("store_id");
            e.Property(p => p.Amount).HasColumnName("amount").HasPrecision(10, 2);
            e.Property(p => p.RetrievedAt).HasColumnName("retrieved_at");
            e.Property(p => p.Volume).HasColumnName("volume").HasMaxLength(100);
            e.Property(p => p.UnitPrice).HasColumnName("unit_price").HasMaxLength(50);
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

        modelBuilder.Entity<Feedback>(e =>
        {
            e.ToTable("feedback");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.UserId).HasColumnName("user_id").HasMaxLength(200);
            e.Property(p => p.UserName).HasColumnName("user_name").HasMaxLength(200);
            e.Property(p => p.Message).HasColumnName("message").IsRequired();
            e.Property(p => p.Category).HasColumnName("category").HasMaxLength(50);
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<ProductGtin>(e =>
        {
            e.ToTable("product_gtins");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.StoreBrand).HasColumnName("store_brand").HasMaxLength(100).IsRequired();
            e.Property(p => p.ExternalProductId).HasColumnName("external_product_id").HasMaxLength(100).IsRequired();
            e.Property(p => p.Gtin).HasColumnName("gtin").HasMaxLength(14);
            e.Property(p => p.ProductName).HasColumnName("product_name").HasMaxLength(500);
            e.Property(p => p.ProductBrand).HasColumnName("product_brand").HasMaxLength(200);
            e.Property(p => p.ProductSize).HasColumnName("product_size").HasMaxLength(100);
            e.Property(p => p.NeedConfirm).HasColumnName("need_confirm").HasDefaultValue(false);
            e.Property(p => p.GtinLookupAttempted).HasColumnName("gtin_lookup_attempted").HasDefaultValue(false);
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            // One row per (store, product); the platform id is unique within a store.
            e.HasIndex(p => new { p.StoreBrand, p.ExternalProductId }).IsUnique();
            // Cross-platform matching is driven by GTIN lookups.
            e.HasIndex(p => p.Gtin);
        });

        // Auckland Supermarket Seed Data
        modelBuilder.Entity<Store>().HasData(
            new Store { Id = 1, Name = "Pak'nSave Royal Oak", Brand = "PakNSave", Latitude = -36.9100, Longitude = 174.7760, Address = "Royal Oak, Auckland" },
            new Store { Id = 2, Name = "New World Victoria Park", Brand = "NewWorld", Latitude = -36.8485, Longitude = 174.7521, Address = "Victoria St West, Auckland CBD" },
            new Store { Id = 3, Name = "Woolworths Auckland City", Brand = "Woolworths", Latitude = -36.8475, Longitude = 174.7670, Address = "Quay St, Auckland CBD" },
            new Store { Id = 4, Name = "Pak'nSave Albany", Brand = "PakNSave", Latitude = -36.7262, Longitude = 174.7061, Address = "Don McKinnon Dr, Albany" },
            new Store { Id = 5, Name = "New World Mt Eden", Brand = "NewWorld", Latitude = -36.8837, Longitude = 174.7622, Address = "Dominion Rd, Mt Eden" },
            new Store { Id = 6, Name = "Woolworths Newmarket", Brand = "Woolworths", Latitude = -36.8687, Longitude = 174.7770, Address = "Broadway, Newmarket" },
            new Store { Id = 7, Name = "Pak'nSave Mt Albert", Brand = "PakNSave", Latitude = -36.8817, Longitude = 174.7188, Address = "New North Rd, Mt Albert" }
        );
    }
}
