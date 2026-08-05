package nz.co.kiwicart.controller;

import nz.co.kiwicart.model.BasketCompareRequest;
import nz.co.kiwicart.model.BasketComparisonResult;
import nz.co.kiwicart.model.PriceResult;
import nz.co.kiwicart.service.PriceComparisonService;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/v1/products")
public class ProductsController {

    private final PriceComparisonService priceComparisonService;

    public ProductsController(PriceComparisonService priceComparisonService) {
        this.priceComparisonService = priceComparisonService;
    }

    @GetMapping("/compare")
    public ResponseEntity<List<PriceResult>> compare(
            @RequestParam(name = "q", defaultValue = "") String query) {

        if (query.isBlank()) {
            return ResponseEntity.ok(List.of());
        }

        var results = priceComparisonService.compare(query.trim());
        return ResponseEntity.ok(results);
    }

    @PostMapping("/compare-bucket")
    public ResponseEntity<List<BasketComparisonResult>> compareBucket(
            @RequestBody BasketCompareRequest request) {

        if (request.getItems() == null || request.getItems().isEmpty()) {
            return ResponseEntity.ok(List.of());
        }

        var results = priceComparisonService.compareBucket(request.getItems());
        return ResponseEntity.ok(results);
    }
}
