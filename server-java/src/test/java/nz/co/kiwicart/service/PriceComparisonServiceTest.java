package nz.co.kiwicart.service;

import nz.co.kiwicart.model.BasketCompareRequest;
import nz.co.kiwicart.model.FoodstuffsConfig;
import nz.co.kiwicart.model.PriceResult;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class PriceComparisonServiceTest {

    @Mock
    private FoodstuffsService foodstuffsService;

    @Mock
    private WoolworthsService woolworthsService;

    @Mock
    private PriceCacheService priceCacheService;

    private PriceComparisonService priceComparisonService;

    private List<PriceResult> paknsaveResults;
    private List<PriceResult> newworldResults;
    private List<PriceResult> woolworthsResults;

    @BeforeEach
    void setUp() {
        UnitPriceCalculator unitPriceCalculator = new UnitPriceCalculator();
        priceComparisonService = new PriceComparisonService(
                foodstuffsService, woolworthsService, priceCacheService, unitPriceCalculator);

        paknsaveResults = List.of(PriceResult.builder()
                .productName("Anchor Milk 2L")
                .price(4.88)
                .supermarketName("Pak'nSave")
                .logoUrl("/images/pak-n-save.webp")
                .imageUrl("https://example.com/milk.png")
                .address("Henderson")
                .lat(-36.88)
                .lng(174.63)
                .build());

        newworldResults = List.of(PriceResult.builder()
                .productName("Anchor Milk 2L")
                .price(5.29)
                .supermarketName("New World")
                .logoUrl("/images/new-world.webp")
                .imageUrl("https://example.com/milk.png")
                .address("Victoria Park")
                .lat(-36.84)
                .lng(174.75)
                .build());

        woolworthsResults = List.of(PriceResult.builder()
                .productName("Anchor Milk 2L")
                .price(5.00)
                .supermarketName("Woolworths")
                .logoUrl("/images/woolworths.webp")
                .imageUrl("https://example.com/milk.png")
                .address("Grey Lynn")
                .lat(-36.86)
                .lng(174.74)
                .build());
    }

    @Test
    void compare_returnsCombinedResultsFromAllStores() {
        // Cache miss
        when(priceCacheService.getCachedResults(any())).thenReturn(Optional.empty());

        when(foodstuffsService.search(eq("Milk"), any(FoodstuffsConfig.class)))
                .thenReturn(paknsaveResults)
                .thenReturn(newworldResults);
        when(woolworthsService.search("Milk"))
                .thenReturn(woolworthsResults);

        var results = priceComparisonService.compare("Milk");

        assertThat(results).hasSize(3);
        assertThat(results).extracting(PriceResult::getSupermarketName)
                .containsExactlyInAnyOrder("Pak'nSave", "New World", "Woolworths");
    }

    @Test
    void compare_returnsCachedResultsWhenAvailable() {
        List<PriceResult> cached = List.of(
                PriceResult.builder().productName("Cached Milk").price(3.99).supermarketName("Pak'nSave").build());
        when(priceCacheService.getCachedResults("Milk")).thenReturn(Optional.of(cached));
        when(priceCacheService.shouldRefreshInBackground("Milk")).thenReturn(false);

        var results = priceComparisonService.compare("Milk");

        assertThat(results).hasSize(1);
        assertThat(results.get(0).getProductName()).isEqualTo("Cached Milk");
    }

    @Test
    void compare_returnsEmptyWhenAllStoresFail() {
        when(priceCacheService.getCachedResults(any())).thenReturn(Optional.empty());
        when(foodstuffsService.search(any(), any(FoodstuffsConfig.class)))
                .thenReturn(List.of());
        when(woolworthsService.search(any()))
                .thenReturn(List.of());

        var results = priceComparisonService.compare("NonExistentProduct");

        assertThat(results).isEmpty();
    }

    @Test
    void compare_calculatesUnitPrice() {
        when(priceCacheService.getCachedResults(any())).thenReturn(Optional.empty());
        when(foodstuffsService.search(eq("Milk"), any(FoodstuffsConfig.class)))
                .thenReturn(paknsaveResults)
                .thenReturn(newworldResults);
        when(woolworthsService.search("Milk"))
                .thenReturn(woolworthsResults);

        var results = priceComparisonService.compare("Milk");

        // "Anchor Milk 2L" → should calculate $/L
        var paknsave = results.stream()
                .filter(r -> r.getSupermarketName().equals("Pak'nSave"))
                .findFirst().orElseThrow();
        assertThat(paknsave.getUnitPrice()).isEqualTo("$2.44/L");
    }

    @Test
    void compareBucket_calculatesTotalPerStore() {
        when(priceCacheService.getCachedResults(any())).thenReturn(Optional.empty());
        when(foodstuffsService.search(eq("Milk"), any(FoodstuffsConfig.class)))
                .thenReturn(paknsaveResults)
                .thenReturn(newworldResults);
        when(woolworthsService.search("Milk"))
                .thenReturn(woolworthsResults);

        var request = new BasketCompareRequest();
        request.setItems(List.of(new BasketCompareRequest.BasketItem("Milk", 2)));

        var results = priceComparisonService.compareBucket(request.getItems());

        assertThat(results).hasSize(3);

        var paknsave = results.stream()
                .filter(r -> r.getSupermarketName().equals("Pak'nSave"))
                .findFirst().orElseThrow();
        assertThat(paknsave.getTotalPrice()).isEqualTo(4.88 * 2);
        assertThat(paknsave.getItemsFound()).isEqualTo(1);
        assertThat(paknsave.getMissingItems()).isEmpty();
    }
}
