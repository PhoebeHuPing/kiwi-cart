package nz.co.kiwicart.service;

import nz.co.kiwicart.model.BasketCompareRequest;
import nz.co.kiwicart.model.BasketComparisonResult;
import nz.co.kiwicart.model.FoodstuffsConfig;
import nz.co.kiwicart.model.PriceResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.concurrent.CompletableFuture;

@Service
public class PriceComparisonService {

    private static final Logger log = LoggerFactory.getLogger(PriceComparisonService.class);

    private final FoodstuffsService foodstuffsService;
    private final WoolworthsService woolworthsService;
    private final PriceCacheService priceCacheService;
    private final UnitPriceCalculator unitPriceCalculator;

    private static final FoodstuffsConfig PAKNSAVE_CONFIG = FoodstuffsConfig.builder()
            .domain("paknsave.co.nz")
            .apiDomain("api-prod.paknsave.co.nz")
            .storeId("65defcf2-bc15-490e-a84f-1f13b769cd22")
            .supermarketName("Pak'nSave")
            .logoUrl("/images/pak-n-save.webp")
            .defaultAddress("Henderson, West Auckland")
            .defaultLat(-36.8819)
            .defaultLng(174.6336)
            .build();

    private static final FoodstuffsConfig NEWWORLD_CONFIG = FoodstuffsConfig.builder()
            .domain("newworld.co.nz")
            .apiDomain("api-prod.newworld.co.nz")
            .storeId("dbdfdd2a-55f7-4870-9b51-979286323647")
            .supermarketName("New World")
            .logoUrl("/images/new-world.webp")
            .defaultAddress("Victoria Park, Auckland")
            .defaultLat(-36.8485)
            .defaultLng(174.7523)
            .build();

    public PriceComparisonService(FoodstuffsService foodstuffsService,
                                  WoolworthsService woolworthsService,
                                  PriceCacheService priceCacheService,
                                  UnitPriceCalculator unitPriceCalculator) {
        this.foodstuffsService = foodstuffsService;
        this.woolworthsService = woolworthsService;
        this.priceCacheService = priceCacheService;
        this.unitPriceCalculator = unitPriceCalculator;
    }

    public List<PriceResult> compare(String query) {
        log.info("Comparing prices for: {}", query);

        // 1. Check cache first
        Optional<List<PriceResult>> cached = priceCacheService.getCachedResults(query);
        if (cached.isPresent()) {
            // Trigger background refresh if cache is stale (> 12h)
            if (priceCacheService.shouldRefreshInBackground(query)) {
                refreshInBackground(query);
            }
            return cached.get();
        }

        // 2. Cache miss - fetch from APIs
        List<PriceResult> results = fetchFromApis(query);

        // 3. Calculate unit prices
        results.forEach(r -> r.setUnitPrice(
                unitPriceCalculator.calculate(r.getProductName(), r.getPrice())));

        // 4. Store in cache
        if (!results.isEmpty()) {
            priceCacheService.cacheResults(query, results);
        }

        log.info("Found {} results for '{}'", results.size(), query);
        return results;
    }

    public List<BasketComparisonResult> compareBucket(List<BasketCompareRequest.BasketItem> items) {
        log.info("Comparing bucket with {} items", items.size());

        Map<String, String> storeLogos = Map.of(
                "Pak'nSave", "/images/pak-n-save.webp",
                "New World", "/images/new-world.webp",
                "Woolworths", "/images/woolworths.webp"
        );

        List<BasketComparisonResult> results = new ArrayList<>();

        for (var storeName : List.of("Pak'nSave", "New World", "Woolworths")) {
            List<BasketComparisonResult.ItemDetail> details = new ArrayList<>();
            List<String> missingItems = new ArrayList<>();
            double totalPrice = 0;

            for (var item : items) {
                List<PriceResult> searchResults = compare(item.getName()).stream()
                        .filter(r -> r.getSupermarketName().equals(storeName))
                        .toList();

                if (searchResults.isEmpty()) {
                    missingItems.add(item.getName());
                } else {
                    var cheapest = searchResults.stream()
                            .min((a, b) -> Double.compare(a.getPrice(), b.getPrice()))
                            .get();

                    double subtotal = cheapest.getPrice() * item.getQuantity();
                    totalPrice += subtotal;

                    details.add(BasketComparisonResult.ItemDetail.builder()
                            .name(cheapest.getProductName())
                            .price(cheapest.getPrice())
                            .quantity(item.getQuantity())
                            .subtotal(subtotal)
                            .build());
                }
            }

            results.add(BasketComparisonResult.builder()
                    .supermarketName(storeName)
                    .logoUrl(storeLogos.get(storeName))
                    .totalPrice(totalPrice)
                    .itemsFound(details.size())
                    .missingItems(missingItems)
                    .details(details)
                    .build());
        }

        // Sort: most items found (descending), then lowest total price (ascending)
        results.sort((a, b) -> {
            int itemCompare = Integer.compare(b.getItemsFound(), a.getItemsFound());
            if (itemCompare != 0) return itemCompare;
            return Double.compare(a.getTotalPrice(), b.getTotalPrice());
        });

        return results;
    }

    @Async
    public void refreshInBackground(String query) {
        log.debug("Background refresh for '{}'", query);
        try {
            List<PriceResult> results = fetchFromApis(query);
            results.forEach(r -> r.setUnitPrice(
                    unitPriceCalculator.calculate(r.getProductName(), r.getPrice())));
            if (!results.isEmpty()) {
                priceCacheService.cacheResults(query, results);
            }
        } catch (Exception e) {
            log.warn("Background refresh failed for '{}': {}", query, e.getMessage());
        }
    }

    private List<PriceResult> fetchFromApis(String query) {
        var paknsaveFuture = CompletableFuture.supplyAsync(() ->
                foodstuffsService.search(query, PAKNSAVE_CONFIG));

        var newworldFuture = CompletableFuture.supplyAsync(() ->
                foodstuffsService.search(query, NEWWORLD_CONFIG));

        var woolworthsFuture = CompletableFuture.supplyAsync(() ->
                woolworthsService.search(query));

        CompletableFuture.allOf(paknsaveFuture, newworldFuture, woolworthsFuture).join();

        var results = new ArrayList<PriceResult>();
        results.addAll(paknsaveFuture.join());
        results.addAll(newworldFuture.join());
        results.addAll(woolworthsFuture.join());
        return results;
    }
}
