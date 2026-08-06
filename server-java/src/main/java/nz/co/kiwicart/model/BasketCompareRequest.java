package nz.co.kiwicart.model;

import java.util.List;

public class BasketCompareRequest {
    private List<BasketItem> items;

    public BasketCompareRequest() {}

    public List<BasketItem> getItems() { return items; }
    public void setItems(List<BasketItem> items) { this.items = items; }

    public static class BasketItem {
        private String name;
        private int quantity;

        public BasketItem() {}

        public BasketItem(String name, int quantity) {
            this.name = name;
            this.quantity = quantity;
        }

        public String getName() { return name; }
        public void setName(String name) { this.name = name; }
        public int getQuantity() { return quantity; }
        public void setQuantity(int quantity) { this.quantity = quantity; }
    }
}
