package nz.co.kiwicart.service;

import nz.co.kiwicart.entity.StoreToken;
import nz.co.kiwicart.repository.StoreTokenRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Three-tier token management:
 * 1. In-memory cache (fastest)
 * 2. Database (survives restarts, shared across instances)
 * 3. External API (refresh when expired)
 */
@Service
public class TokenPersistenceService {

    private static final Logger log = LoggerFactory.getLogger(TokenPersistenceService.class);

    private final StoreTokenRepository tokenRepository;
    private final ConcurrentHashMap<String, CachedToken> memoryCache = new ConcurrentHashMap<>();

    public TokenPersistenceService(StoreTokenRepository tokenRepository) {
        this.tokenRepository = tokenRepository;
    }

    /**
     * Get token from memory → DB → null (caller must fetch from API)
     */
    public Optional<String> getToken(String storeBrand) {
        // Tier 1: Memory cache
        CachedToken cached = memoryCache.get(storeBrand);
        if (cached != null && !cached.isExpired()) {
            return Optional.of(cached.token());
        }

        // Tier 2: Database
        Optional<StoreToken> dbToken = tokenRepository.findByStoreBrand(storeBrand);
        if (dbToken.isPresent()) {
            StoreToken st = dbToken.get();
            if (st.getExpiresAt() == null || st.getExpiresAt().isAfter(LocalDateTime.now())) {
                // Populate memory cache
                memoryCache.put(storeBrand, new CachedToken(st.getToken(), st.getExpiresAt()));
                log.debug("Token for '{}' loaded from database", storeBrand);
                return Optional.of(st.getToken());
            }
            log.debug("Token for '{}' expired in database", storeBrand);
        }

        // Tier 3: Not available — caller must fetch from API
        return Optional.empty();
    }

    /**
     * Store token in both memory and database.
     */
    @Transactional
    public void storeToken(String storeBrand, String token, LocalDateTime expiresAt) {
        // Update memory
        memoryCache.put(storeBrand, new CachedToken(token, expiresAt));

        // Update database (upsert)
        Optional<StoreToken> existing = tokenRepository.findByStoreBrand(storeBrand);
        StoreToken entity;
        if (existing.isPresent()) {
            entity = existing.get();
            entity.setToken(token);
            entity.setExpiresAt(expiresAt);
        } else {
            entity = StoreToken.builder()
                    .storeBrand(storeBrand)
                    .token(token)
                    .expiresAt(expiresAt)
                    .build();
        }
        tokenRepository.save(entity);
        log.info("Token persisted for '{}'", storeBrand);
    }

    /**
     * Invalidate token (memory + database)
     */
    @Transactional
    public void invalidateToken(String storeBrand) {
        memoryCache.remove(storeBrand);
        tokenRepository.findByStoreBrand(storeBrand).ifPresent(tokenRepository::delete);
        log.debug("Token invalidated for '{}'", storeBrand);
    }

    private record CachedToken(String token, LocalDateTime expiresAt) {
        boolean isExpired() {
            return expiresAt != null && expiresAt.isBefore(LocalDateTime.now());
        }
    }
}
