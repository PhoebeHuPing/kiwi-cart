package nz.co.kiwicart.controller;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import nz.co.kiwicart.entity.Store;
import nz.co.kiwicart.repository.StoreRepository;
import nz.co.kiwicart.util.GeoUtils;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/v1/stores")
@Tag(name = "Stores", description = "Supermarket store locations")
public class StoresController {

    private final StoreRepository storeRepository;

    public StoresController(StoreRepository storeRepository) {
        this.storeRepository = storeRepository;
    }

    @GetMapping
    @Operation(summary = "List all stores", description = "Get all supermarket store locations")
    @Cacheable(value = "supermarkets")
    public ResponseEntity<List<Store>> getAllStores() {
        return ResponseEntity.ok(storeRepository.findAll());
    }

    @GetMapping("/nearby")
    @Operation(summary = "Find nearby stores", description = "Find stores within a radius of given coordinates")
    public ResponseEntity<?> getNearbyStores(
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
}
