package nz.co.kiwicart.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Table(name = "price_cache")
@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class PriceCache {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "search_query", nullable = false)
    private String searchQuery;

    @Column(name = "supermarket_name", nullable = false)
    private String supermarketName;

    @Column(name = "product_name", nullable = false)
    private String productName;

    @Column(name = "image_url")
    private String imageUrl;

    @Column(name = "logo_url")
    private String logoUrl;

    private String address;

    private Double lat;

    private Double lng;

    private double price;

    @Column(name = "unit_price")
    private String unitPrice;

    @Column(name = "cached_at", nullable = false)
    private LocalDateTime cachedAt;

    @PrePersist
    protected void onCreate() {
        cachedAt = LocalDateTime.now();
    }
}
