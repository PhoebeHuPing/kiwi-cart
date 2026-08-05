package nz.co.kiwicart.model;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class PriceResult {

    @JsonProperty("product_name")
    private String productName;

    @JsonProperty("image_url")
    private String imageUrl;

    @JsonProperty("supermarket_name")
    private String supermarketName;

    @JsonProperty("logo_url")
    private String logoUrl;

    private String address;

    private double lat;

    private double lng;

    private double price;

    @JsonProperty("unit_price")
    private String unitPrice;
}
