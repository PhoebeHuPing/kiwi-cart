package nz.co.kiwicart.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtException;
import org.springframework.security.web.SecurityFilterChain;

import java.time.Instant;
import java.util.List;
import java.util.Map;

/**
 * Development-only security configuration.
 * Allows running locally without a real Auth0 tenant.
 * All endpoints are accessible; JWT validation is bypassed.
 *
 * Activate with: --spring.profiles.active=dev
 */
@Configuration
@EnableWebSecurity
@Profile("dev")
public class DevSecurityConfig {

    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http
                .cors(cors -> {})
                .csrf(csrf -> csrf.disable())
                .authorizeHttpRequests(auth -> auth.anyRequest().permitAll())
                .oauth2ResourceServer(oauth2 -> oauth2
                        .jwt(jwt -> jwt.decoder(devJwtDecoder()))
                );
        return http.build();
    }

    @Bean
    public JwtDecoder devJwtDecoder() {
        // Return a no-op decoder that accepts any token for local dev
        return token -> {
            return new Jwt(
                    token,
                    Instant.now(),
                    Instant.now().plusSeconds(3600),
                    Map.of("alg", "none"),
                    Map.of(
                            "sub", "dev-user|123",
                            "aud", List.of("https://kiwicart/api"),
                            "name", "Dev User"
                    )
            );
        };
    }
}
