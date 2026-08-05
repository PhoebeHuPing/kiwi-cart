package nz.co.kiwicart.model;

import lombok.Data;

import java.util.List;

@Data
public class BasketCompareRequest {
    private List<BasketItem> items;

    @Data
    public static class BasketItem {
        private String name;
        private int quantity;
    }
}
