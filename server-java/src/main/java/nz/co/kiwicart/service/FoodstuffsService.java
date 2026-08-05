package nz.co.kiwicart.service;

import nz.co.kiwicart.model.FoodstuffsConfig;
import nz.co.kiwicart.model.PriceResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.web.reactive.function.client.WebClient;

import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class FoodstuffsService {

    private static final Logger log = LoggerFactory.getLogger(FoodstuffsService.class);
    private static final String USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private final WebClient.Builder webClientBuilder;
    private final ConcurrentHashMap<String, String> tokenCache = new ConcurrentHashMap<>();

    public FoodstuffsService(WebClient.Builder webClientBuilder) {
        this.webClientBuilder = webClientBuilder;
    }

    public List<PriceResult> search(String searchTerm, FoodstuffsConfig config) {
        try {
            String token = getToken(config);
            List<PriceResult> results = doSearch(searchTerm, token, config);

            if (results == null) {
                log.warn("Token for {} expired, refreshing...", config.getDomain());
                tokenCache.remove(config.getDomain());
                token = getToken(config);
                results = doSearch(searchTerm, token, config);
            }

            return results != null ? results : Collections.emptyList();
        } catch (Exception e) {
            log.error("{} Real-time API failed: {}", config.getSupermarketName(), e.getMessage());
            return Collections.emptyList();
        }
    }

    private String getToken(FoodstuffsConfig config) {
        return tokenCache.computeIfAbsent(config.getDomain(), domain -> {
            log.info("Fetching token for {}", domain);

            var client = webClientBuilder.build();
            var response = client.post()
                    .uri("https://www." + domain + "/api/user/get-current-user")
                    .header("User-Agent", USER_AGENT)
                    .header("Content-Type", "application/json")
                    .bodyValue(Map.of())
                    .retrieve()
                    .bodyToMono(Map.class)
                    .block();

            if (response == null || !response.containsKey("access_token")) {
                throw new RuntimeException("No token returned from " + domain);
            }

            String token = (String) response.get("access_token");
            log.info("Successfully refreshed token for {}", domain);
            return token;
        });
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
                    .block();
        } catch (Exception e) {
            if (e.getMessage() != null && e.getMessage().contains("401")) {
                return null;
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
