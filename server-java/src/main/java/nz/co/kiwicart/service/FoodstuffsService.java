package nz.co.kiwicart.service;

import io.github.resilience4j.circuitbreaker.annotation.CircuitBreaker;
import io.github.resilience4j.retry.annotation.Retry;
import nz.co.kiwicart.model.FoodstuffsConfig;
import nz.co.kiwicart.model.PriceResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.web.reactive.function.client.WebClient;

import java.time.Duration;
import java.time.LocalDateTime;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.Optional;

@Service
public class FoodstuffsService {

    private static final Logger log = LoggerFactory.getLogger(FoodstuffsService.class);
    private static final String USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private final WebClient.Builder webClientBuilder;
    private final TokenPersistenceService tokenPersistenceService;

    public FoodstuffsService(WebClient.Builder webClientBuilder, TokenPersistenceService tokenPersistenceService) {
        this.webClientBuilder = webClientBuilder;
        this.tokenPersistenceService = tokenPersistenceService;
    }

    @CircuitBreaker(name = "foodstuffs", fallbackMethod = "searchFallback")
    @Retry(name = "foodstuffs")
    public List<PriceResult> search(String searchTerm, FoodstuffsConfig config) {
        try {
            String token = getToken(config);
            List<PriceResult> results = doSearch(searchTerm, token, config);

            if (results == null) {
                // Token expired in-flight — invalidate and refresh
                log.warn("Token for {} expired, refreshing...", config.getDomain());
                tokenPersistenceService.invalidateToken(config.getDomain());
                token = fetchAndStoreToken(config);
                results = doSearch(searchTerm, token, config);
            }

            return results != null ? results : Collections.emptyList();
        } catch (Exception e) {
            log.error("{} Real-time API failed: {}", config.getSupermarketName(), e.getMessage());
            throw e;
        }
    }

    @SuppressWarnings("unused")
    private List<PriceResult> searchFallback(String searchTerm, FoodstuffsConfig config, Throwable t) {
        log.warn("Circuit breaker fallback for {} search '{}': {}",
                config.getSupermarketName(), searchTerm, t.getMessage());
        return Collections.emptyList();
    }

    private String getToken(FoodstuffsConfig config) {
        // Try 3-tier: memory → DB → API
        Optional<String> cached = tokenPersistenceService.getToken(config.getDomain());
        if (cached.isPresent()) {
            return cached.get();
        }
        return fetchAndStoreToken(config);
    }

    private String fetchAndStoreToken(FoodstuffsConfig config) {
        log.info("Fetching token from API for {}", config.getDomain());

        var client = webClientBuilder.build();
        var response = client.post()
                .uri("https://www." + config.getDomain() + "/api/user/get-current-user")
                .header("User-Agent", USER_AGENT)
                .header("Content-Type", "application/json")
                .bodyValue(Map.of())
                .retrieve()
                .bodyToMono(Map.class)
                .timeout(Duration.ofSeconds(10))
                .block();

        if (response == null || !response.containsKey("access_token")) {
            throw new RuntimeException("No token returned from " + config.getDomain());
        }

        String token = (String) response.get("access_token");

        // Tokens typically last ~1 hour, set expiry to 50 minutes for safety
        LocalDateTime expiresAt = LocalDateTime.now().plusMinutes(50);
        tokenPersistenceService.storeToken(config.getDomain(), token, expiresAt);

        log.info("Successfully refreshed token for {}", config.getDomain());
        return token;
    }

    @SuppressWarnings("unchecked")
    private List<PriceResult> doSearch(String searchTerm, String token, FoodstuffsConfig config) {
        var client = webClientBuilder.build();

        Map<String, Object> body = Map.of(
                "algoliaQuery", Map.of("query", searchTerm),
                "storeId", config.getStoreId(),
                "hitsPerPage", 50,
                "page", 0,
                "sortOrder", "NI_POPULARITY_ASC"
        );

        Map<String, Object> response;
        try {
            response = client.post()
                    .uri("https://" + config.getApiDomain() + "/v1/edge/search/paginated/products")
                    .header("Authorization", "Bearer " + token)
                    .header("Content-Type", "application/json")
                    .header("User-Agent", USER_AGENT)
                    .bodyValue(body)
                    .retrieve()
                    .bodyToMono(Map.class)
                    .timeout(Duration.ofSeconds(10))
                    .block();
        } catch (Exception e) {
            if (e.getMessage() != null && e.getMessage().contains("401")) {
                return null; // Signal token expired
            }
            throw e;
        }

        if (response == null) return Collections.emptyList();

        List<Map<String, Object>> products = (List<Map<String, Object>>) response.get("products");
        if (products == null) return Collections.emptyList();

        return products.stream()
                .map(p -> mapToResult(p, config))
                .toList();
    }

    @SuppressWarnings("unchecked")
    private PriceResult mapToResult(Map<String, Object> product, FoodstuffsConfig config) {
        String name = (String) product.getOrDefault("name", "");
        String productId = (String) product.getOrDefault("productId", "");
        String simpleId = productId.contains("-") ? productId.split("-")[0] : productId;

        String imageUrl = null;
        Map<String, Object> images = (Map<String, Object>) product.get("images");
        if (images != null) {
            Map<String, Object> primaryImages = (Map<String, Object>) images.get("primaryImages");
            if (primaryImages != null) {
                imageUrl = (String) primaryImages.get("400px");
            }
        }
        if (imageUrl == null || imageUrl.isEmpty()) {
            imageUrl = "https://a.fsimg.co.nz/product/retail/fan/image/400x400/" + simpleId + ".png";
        }

        double price = 0;
        Map<String, Object> singlePrice = (Map<String, Object>) product.get("singlePrice");
        if (singlePrice != null) {
            Object priceObj = singlePrice.get("price");
            if (priceObj instanceof Number) {
                price = ((Number) priceObj).doubleValue() / 100.0;
            }
        }

        return PriceResult.builder()
                .productName(name)
                .imageUrl(imageUrl)
                .supermarketName(config.getSupermarketName())
                .logoUrl(config.getLogoUrl())
                .address(config.getDefaultAddress())
                .lat(config.getDefaultLat())
                .lng(config.getDefaultLng())
                .price(price)
                .build();
    }
}
