package nz.co.kiwicart.controller;

import nz.co.kiwicart.entity.Store;
import nz.co.kiwicart.repository.StoreRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.test.web.servlet.MockMvc;

import static org.hamcrest.Matchers.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest
@AutoConfigureMockMvc
class ProductsControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private StoreRepository storeRepository;

    @MockBean
    private JwtDecoder jwtDecoder;

    @BeforeEach
    void setUp() {
        storeRepository.deleteAll();
    }

    @Test
    void compare_withEmptyQuery_returnsEmptyList() throws Exception {
        mockMvc.perform(get("/api/v1/products/compare").param("q", ""))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$", hasSize(0)));
    }

    @Test
    void supermarkets_returnsAllStores() throws Exception {
        var store = new Store();
        store.setName("Pak'nSave Henderson");
        store.setBrand("Pak'nSave");
        store.setLogoUrl("/images/pak-n-save.webp");
        store.setAddress("Henderson, Auckland");
        store.setLatitude(-36.88);
        store.setLongitude(174.63);
        storeRepository.save(store);

        mockMvc.perform(get("/api/v1/products/supermarkets"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$", hasSize(1)))
                .andExpect(jsonPath("$[0].name", is("Pak'nSave Henderson")));
    }

    @Test
    void nearby_withoutCoordinates_returnsBadRequest() throws Exception {
        mockMvc.perform(get("/api/v1/products/nearby"))
                .andExpect(status().isBadRequest());
    }

    @Test
    void nearby_returnsStoresWithinRadius() throws Exception {
        var nearby = new Store();
        nearby.setName("Close Store");
        nearby.setLatitude(-36.88);
        nearby.setLongitude(174.63);
        storeRepository.save(nearby);

        var farAway = new Store();
        farAway.setName("Far Store");
        farAway.setLatitude(-45.0);
        farAway.setLongitude(170.0);
        storeRepository.save(farAway);

        mockMvc.perform(get("/api/v1/products/nearby")
                        .param("lat", "-36.88")
                        .param("lng", "174.63")
                        .param("radius", "5"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$", hasSize(1)))
                .andExpect(jsonPath("$[0].name", is("Close Store")));
    }

    @Test
    void health_returnsJavaBackendInfo() throws Exception {
        mockMvc.perform(get("/api/v1/health"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.backend", is("java")))
                .andExpect(jsonPath("$.status", is("ok")));
    }

    @Test
    void favorites_withoutAuth_returns401() throws Exception {
        mockMvc.perform(get("/api/v1/products/favorites"))
                .andExpect(status().isUnauthorized());
    }
}
