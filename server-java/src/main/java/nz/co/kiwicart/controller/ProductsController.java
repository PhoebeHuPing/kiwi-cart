package nz.co.kiwicart.controller;

import nz.co.kiwicart.entity.Favorite;
import nz.co.kiwicart.entity.Store;
import nz.co.kiwicart.model.BasketCompareRequest;
import nz.co.kiwicart.model.BasketComparisonResult;
import nz.co.kiwicart.model.PriceResult;
import nz.co.kiwicart.repository.FavoriteRepository;
import nz.co.kiwicart.repository.ProductRepository;
import nz.co.kiwicart.repository.StoreRepository;
import nz.co.kiwicart.service.PriceComparisonService;
import nz.co.kiwicart.util.GeoUtils;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/v1/products")
@Tag(name = "Products", description = "Price comparison and product management")
public class ProductsController {

    private final PriceComparisonService priceComparisonService;
    private final StoreRepository storeRepository;
    private final ProductRepository productRepository;
    private final FavoriteRepository favoriteRepository;

    public ProductsController(
            PriceComparisonService priceComparisonService,
            StoreRepository storeRepository,
            ProductRepository productRepository,
            FavoriteRepository favoriteRepository) {
        this.priceComparisonService = priceComparisonService;
        this.storeRepository = storeRepository;
        this.productRepository = productRepository;
        this.favoriteRepository = favoriteRepository;
    }

    @GetMapping
    @Operation(summary = "List all products", description = "Get all products from the database")
    public ResponseEntity<List<nz.co.kiwicart.entity.Product>> getAllProducts() {
        return ResponseEntity.ok(productRepository.findAll());
    }

    @GetMapping("/compare")
    @Operation(summary = "Compare prices", description = "Search and compare prices across all supermarkets")
    @Cacheable(value = "compareResults", key = "#query", condition = "#query != null && !#query.isBlank()")
    public ResponseEntity<List<PriceResult>> compare(
            @RequestParam(name = "q", defaultValue = "") String query) {
        if (query.isBlank()) {
            return ResponseEntity.ok(List.of());
        }
        return ResponseEntity.ok(priceComparisonService.compare(query.trim()));
    }

    @PostMapping("/compare-bucket")
    @Operation(summary = "Compare basket", description = "Compare total basket price across supermarkets")
    public ResponseEntity<List<BasketComparisonResult>> compareBucket(
            @RequestBody BasketCompareRequest request) {
        if (request.getItems() == null || request.getItems().isEmpty()) {
            return ResponseEntity.ok(List.of());
        }
        return ResponseEntity.ok(priceComparisonService.compareBucket(request.getItems()));
    }

    @GetMapping("/supermarkets")
    @Operation(summary = "List stores", description = "Get all supermarket store locations")
    @Cacheable(value = "supermarkets")
    public ResponseEntity<List<Store>> getSupermarkets() {
        return ResponseEntity.ok(storeRepository.findAll());
    }

    @GetMapping("/nearby")
    @Operation(summary = "Find nearby stores", description = "Find stores within a radius of given coordinates")
    public ResponseEntity<?> getNearby(
            @RequestParam(required = false) Double lat,
            @RequestParam(required = false) Double lng,
            @RequestParam(defaultValue = "5") double radius) {
        if (lat == null || lng == null) {
            return ResponseEntity.badRequest().body(Map.of("error", "lat and lng query parameters are required."));
        }
        var allStores = storeRepository.findAll();
        var nearby = allStores.stream()
                .filter(store -> store.getLatitude() != null && store.getLongitude() != null)
                .filter(store -> GeoUtils.calculateDistanceKm(lat, lng, store.getLatitude(), store.getLongitude()) <= radius)
                .toList();
        return ResponseEntity.ok(nearby);
    }

    @GetMapping("/favorites")
    @Operation(summary = "Get favorites", description = "Get user's favorite products (requires auth)")
    public ResponseEntity<List<String>> getFavorites(@AuthenticationPrincipal Jwt jwt) {
        String userId = jwt.getSubject();
        var favorites = favoriteRepository.findByUserIdOrderByCreatedAtDesc(userId);
        return ResponseEntity.ok(favorites.stream().map(Favorite::getProductName).toList());
    }

    @PostMapping("/favorites")
    @Operation(summary = "Toggle favorite", description = "Add or remove a product from favorites (requires auth)")
    @Transactional
    public ResponseEntity<Map<String, String>> toggleFavorite(
            @AuthenticationPrincipal Jwt jwt,
            @RequestBody Map<String, String> body) {
        String userId = jwt.getSubject();
        String name = body.get("name");

        if (name == null || name.isBlank()) {
            return ResponseEntity.badRequest().body(Map.of("error", "Product name is required"));
        }

        var existing = favoriteRepository.findByUserIdAndProductName(userId, name);
        if (existing.isPresent()) {
            favoriteRepository.deleteByUserIdAndProductName(userId, name);
            return ResponseEntity.ok(Map.of("action", "removed", "name", name));
        } else {
            var favorite = new Favorite();
            favorite.setUserId(userId);
            favorite.setProductName(name);
            favoriteRepository.save(favorite);
            return ResponseEntity.ok(Map.of("action", "added", "name", name));
        }
    }

    @DeleteMapping("/{id}")
    @Operation(summary = "Delete product", description = "Delete a product (admin only)")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<Void> deleteProduct(@PathVariable Long id) {
        if (!productRepository.existsById(id)) {
            return ResponseEntity.notFound().build();
        }
        productRepository.deleteById(id);
        return ResponseEntity.noContent().build();
    }

}
