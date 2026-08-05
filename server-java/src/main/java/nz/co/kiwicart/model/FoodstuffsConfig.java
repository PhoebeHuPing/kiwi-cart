package nz.co.kiwicart.model;

import lombok.Builder;
import lombok.Data;

@Data
@Builder
public class FoodstuffsConfig {
    private String domain;
    private String apiDomain;
    private String storeId;
    private String supermarketName;
    private String logoUrl;
    private String defaultAddress;
    private double defaultLat;
    private double defaultLng;
}
