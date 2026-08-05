package nz.co.kiwicart.model;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class BasketComparisonResult {

    @JsonProperty("supermarket_name")
    private String supermarketName;

    @JsonProperty("logo_url")
    private String logoUrl;

    @JsonProperty("total_price")
    private double totalPrice;

    @JsonProperty("items_found")
    private int itemsFound;

    @JsonProperty("missing_items")
    private List<String> missingItems;

    private List<ItemDetail> details;

    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class ItemDetail {
        private String name;
        private double price;
        private int quantity;
        private double subtotal;
    }
}
