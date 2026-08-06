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
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/v1/products")
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

    @GetMapping("/compare")
    public ResponseEntity<List<PriceResult>> compare(
            @RequestParam(name = "q", defaultValue = "") String query) {
        if (query.isBlank()) {
            return ResponseEntity.ok(List.of());
        }
        return ResponseEntity.ok(priceComparisonService.compare(query.trim()));
    }

    @PostMapping("/compare-bucket")
    public ResponseEntity<List<BasketComparisonResult>> compareBucket(
            @RequestBody BasketCompareRequest request) {
        if (request.getItems() == null || request.getItems().isEmpty()) {
            return ResponseEntity.ok(List.of());
        }
        return ResponseEntity.ok(priceComparisonService.compareBucket(request.getItems()));
    }

    @GetMapping("/supermarkets")
    public ResponseEntity<List<Store>> getSupermarkets() {
        return ResponseEntity.ok(storeRepository.findAll());
    }

    @GetMapping("/nearby")
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
                .filter(store -> calculateDistance(lat, lng, store.getLatitude(), store.getLongitude()) <= radius)
                .toList();
        return ResponseEntity.ok(nearby);
    }

    @GetMapping("/favorites")
    public ResponseEntity<List<String>> getFavorites(@AuthenticationPrincipal Jwt jwt) {
        String userId = jwt.getSubject();
        var favorites = favoriteRepository.findByUserIdOrderByCreatedAtDesc(userId);
        return ResponseEntity.ok(favorites.stream().map(Favorite::getProductName).toList());
    }

    @PostMapping("/favorites")
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
    public ResponseEntity<Void> deleteProduct(@PathVariable Long id) {
        if (!productRepository.existsById(id)) {
            return ResponseEntity.notFound().build();
        }
        productRepository.deleteById(id);
        return ResponseEntity.noContent().build();
    }

    private double calculateDistance(double lat1, double lon1, double lat2, double lon2) {
        double R = 6371;
        double dLat = Math.toRadians(lat2 - lat1);
        double dLon = Math.toRadians(lon2 - lon1);
        double a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
                Math.cos(Math.toRadians(lat1)) * Math.cos(Math.toRadians(lat2)) *
                        Math.sin(dLon / 2) * Math.sin(dLon / 2);
        double c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        return R * c;
    }
}
