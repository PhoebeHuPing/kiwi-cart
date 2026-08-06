package nz.co.kiwicart.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import javax.sql.DataSource;
import java.sql.Connection;
import java.util.LinkedHashMap;
import java.util.Map;

@RestController
@RequestMapping("/api/v1")
public class HealthController {

    private final DataSource dataSource;

    public HealthController(DataSource dataSource) {
        this.dataSource = dataSource;
    }

    @GetMapping("/health")
    public ResponseEntity<Map<String, Object>> health() {
        Map<String, Object> response = new LinkedHashMap<>();
        response.put("status", "ok");
        response.put("backend", "java");
        response.put("framework", "Spring Boot 3.3.2");

        // Check database connectivity
        try (Connection conn = dataSource.getConnection()) {
            response.put("database", conn.isValid(3) ? "connected" : "error");
        } catch (Exception e) {
            response.put("database", "error");
            response.put("status", "degraded");
        }

        return ResponseEntity.ok(response);
    }
}
