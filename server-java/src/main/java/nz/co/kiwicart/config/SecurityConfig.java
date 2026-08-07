package nz.co.kiwicart.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;
import org.springframework.core.convert.converter.Converter;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.oauth2.core.DelegatingOAuth2TokenValidator;
import org.springframework.security.oauth2.core.OAuth2Error;
import org.springframework.security.oauth2.core.OAuth2TokenValidator;
import org.springframework.security.oauth2.core.OAuth2TokenValidatorResult;
import org.springframework.security.oauth2.jwt.*;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationConverter;
import org.springframework.security.web.SecurityFilterChain;

import java.util.Collection;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Configuration
@EnableWebSecurity
@EnableMethodSecurity
@Profile("!dev")
public class SecurityConfig {

    @Value("${spring.security.oauth2.resourceserver.jwt.issuer-uri}")
    private String issuerUri;

    @Value("${spring.security.oauth2.resourceserver.jwt.audiences}")
    private String audience;

    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http
                .cors(cors -> {})
                .csrf(csrf -> csrf.disable())
                .authorizeHttpRequests(auth -> auth
                        // Public API endpoints
                        .requestMatchers(HttpMethod.GET, "/api/v1/products").permitAll()
                        .requestMatchers(HttpMethod.GET, "/api/v1/products/compare").permitAll()
                        .requestMatchers(HttpMethod.POST, "/api/v1/products/compare-bucket").permitAll()
                        .requestMatchers(HttpMethod.GET, "/api/v1/products/supermarkets").permitAll()
                        .requestMatchers(HttpMethod.GET, "/api/v1/products/nearby").permitAll()
                        .requestMatchers(HttpMethod.GET, "/api/v1/stores/**").permitAll()
                        .requestMatchers(HttpMethod.GET, "/api/v1/health").permitAll()
                        .requestMatchers(HttpMethod.GET, "/api/v1/feedback").permitAll()
                        // Actuator & Swagger
                        .requestMatchers("/actuator/**").permitAll()
                        .requestMatchers("/swagger-ui/**", "/v3/api-docs/**", "/swagger-ui.html").permitAll()
                        // Static resources
                        .requestMatchers("/", "/index.html", "/assets/**", "/images/**", "/favicon.svg").permitAll()
                        // Authenticated endpoints
                        .requestMatchers("/api/v1/products/favorites/**").authenticated()
                        .requestMatchers(HttpMethod.POST, "/api/v1/feedback").authenticated()
                        // Admin-only: delete products
                        .requestMatchers(HttpMethod.DELETE, "/api/v1/products/**").hasRole("ADMIN")
                        .anyRequest().permitAll()
                )
                .oauth2ResourceServer(oauth2 -> oauth2
                        .jwt(jwt -> jwt
                                .decoder(jwtDecoder())
                                .jwtAuthenticationConverter(jwtAuthenticationConverter())
                        )
                );

        return http.build();
    }

    @Bean
    public JwtDecoder jwtDecoder() {
        // Lazy-loaded: does NOT connect to Auth0 at startup.
        // OIDC discovery happens on first token validation request,
        // matching the behavior of ASP.NET Core's AddJwtBearer.
        return new JwtDecoder() {
            private volatile NimbusJwtDecoder delegate;

            @Override
            public Jwt decode(String token) throws JwtException {
                if (delegate == null) {
                    synchronized (this) {
                        if (delegate == null) {
                            NimbusJwtDecoder decoder = JwtDecoders.fromIssuerLocation(issuerUri);

                            OAuth2TokenValidator<Jwt> audienceValidator = t ->
                                    t.getAudience().contains(audience)
                                            ? OAuth2TokenValidatorResult.success()
                                            : OAuth2TokenValidatorResult.failure(
                                            new OAuth2Error("invalid_token", "The required audience is missing", null));

                            OAuth2TokenValidator<Jwt> withIssuer = JwtValidators.createDefaultWithIssuer(issuerUri);
                            OAuth2TokenValidator<Jwt> combined = new DelegatingOAuth2TokenValidator<>(withIssuer, audienceValidator);

                            decoder.setJwtValidator(combined);
                            this.delegate = decoder;
                        }
                    }
                }
                return delegate.decode(token);
            }
        };
    }

    @Bean
    public JwtAuthenticationConverter jwtAuthenticationConverter() {
        JwtAuthenticationConverter converter = new JwtAuthenticationConverter();
        converter.setJwtGrantedAuthoritiesConverter(new Auth0RoleConverter());
        return converter;
    }

    /**
     * Extracts roles from Auth0 JWT claims.
     * Auth0 stores roles in a custom namespace claim like:
     * "https://kiwicart.co.nz/roles": ["admin"]
     */
    private static class Auth0RoleConverter implements Converter<Jwt, Collection<GrantedAuthority>> {
        private static final String ROLES_CLAIM = "https://kiwicart.co.nz/roles";

        @Override
        @SuppressWarnings("unchecked")
        public Collection<GrantedAuthority> convert(Jwt jwt) {
            Object rolesClaim = jwt.getClaim(ROLES_CLAIM);
            if (rolesClaim == null) {
                // Also check permissions claim
                List<String> permissions = jwt.getClaimAsStringList("permissions");
                if (permissions != null) {
                    return permissions.stream()
                            .map(p -> new SimpleGrantedAuthority("ROLE_" + p.toUpperCase()))
                            .collect(Collectors.toList());
                }
                return Collections.emptyList();
            }

            List<String> roles;
            if (rolesClaim instanceof List) {
                roles = (List<String>) rolesClaim;
            } else {
                return Collections.emptyList();
            }

            return roles.stream()
                    .map(role -> new SimpleGrantedAuthority("ROLE_" + role.toUpperCase()))
                    .collect(Collectors.toList());
        }
    }
}
