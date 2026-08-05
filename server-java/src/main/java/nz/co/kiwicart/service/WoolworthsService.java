package nz.co.kiwicart.service;

import nz.co.kiwicart.model.PriceResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.web.reactive.function.client.WebClient;

import java.util.Collections;
import java.util.List;
import java.util.Map;

@Service
public class WoolworthsService {

    private static final Logger log = LoggerFactory.getLogger(WoolworthsService.class);
    private static final String BASE_URL = "https://www.woolworths.co.nz";
    private static final String USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private final WebClient.Builder webClientBuilder;

    public WoolworthsService(WebClient.Builder webClientBuilder) {
        this.webClientBuilder = webClientBuilder;
    }

    @SuppressWarnings("unchecked")
    public List<PriceResult> search(String searchTerm) {
        try {
            var client = webClientBuilder
                    .baseUrl(BASE_URL)
                    .build();

            var sessionResponse = client.post()
                    .uri("/api/v1/session")
                    .header("User-Agent", USER_AGENT)
                    .header("X-Requested-With", "OnlineShopping.WebApp")
                    .bodyValue(Map.of())
                    .exchangeToMono(response -> {
                        List<String> cookies = response.headers().header("Set-Cookie");
                        return response.bodyToMono(String.class)
                                .map(body -> cookies);
                    })
                    .block();

            String cookieHeader = sessionResponse != null
                    ? String.join("; ", sessionResponse.stream()
                            .map(c -> c.split(";")[0])
                            .toList())
                    : "";

            var response = client.get()
                    .uri(uriBuilder -> uriBuilder
                            .path("/api/v1/products")
                            .queryParam("target", "search")
                            .queryParam("search", searchTerm)
                            .queryParam("inStockProductsOnly", true)
                            .build())
                    .header("User-Agent", USER_AGENT)
                    .header("X-Requested-With", "OnlineShopping.WebApp")
                    .header("Accept", "application/json")
                    .header("Cookie", cookieHeader)
                    .retrieve()
                    .bodyToMono(Map.class)
                    .block();

            if (response == null) return Collections.emptyList();

            Map<String, Object> productsObj = (Map<String, Object>) response.get("products");
            if (productsObj == null) return Collections.emptyList();

            List<Map<String, Object>> items = (List<Map<String, Object>>) productsObj.get("items");
            if (items == null) return Collections.emptyList();

            return items.stream()
                    .filter(item -> "Product".equals(item.get("type")))
                    .map(this::mapToResult)
                    .toList();

        } catch (Exception e) {
            log.error("Woolworths Real-time API failed: {}", e.getMessage());
            return Collections.emptyList();
        }
    }

    @SuppressWarnings("unchecked")
    private PriceResult mapToResult(Map<String, Object> product) {
        String name = (String) product.getOrDefault("name", "");

        String imageUrl = "";
        Map<String, Object> images = (Map<String, Object>) product.get("images");
        if (images != null) {
            String bigImage = (String) images.get("big");
            String smallImage = (String) images.get("small");
            if (bigImage != null) {
                imageUrl = bigImage.replace("w=200&h=200", "w=400&h=400");
            } else if (smallImage != null) {
                imageUrl = smallImage;
            }
        }

        double price = 0;
        Map<String, Object> priceObj = (Map<String, Object>) product.get("price");
        if (priceObj != null) {
            Object salePrice = priceObj.get("salePrice");
            if (salePrice instanceof Number) {
                price = ((Number) salePrice).doubleValue();
            }
        }

        return PriceResult.builder()
                .productName(name)
                .imageUrl(imageUrl)
                .supermarketName("Woolworths")
                .logoUrl("/images/woolworths.webp")
                .address("Grey Lynn, Auckland")
                .lat(-36.8645)
                .lng(174.7431)
                .price(price)
                .build();
    }
}
