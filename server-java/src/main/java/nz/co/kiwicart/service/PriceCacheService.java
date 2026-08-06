package nz.co.kiwicart.service;

import nz.co.kiwicart.entity.PriceCache;
import nz.co.kiwicart.model.PriceResult;
import nz.co.kiwicart.repository.PriceCacheRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Async;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

/**
 * Hybrid caching strategy:
 * 1. Check DB cache first (24-hour expiry)
 * 2. If cache hit → return immediately, trigger background refresh if > 12h old
 * 3. If cache miss → fetch from API, store in DB, return results
 */
@Service
public class PriceCacheService {

    private static final Logger log = LoggerFactory.getLogger(PriceCacheService.class);
    private static final int CACHE_EXPIRY_HOURS = 24;
    private static final int REFRESH_THRESHOLD_HOURS = 12;

    private final PriceCacheRepository cacheRepository;

    public PriceCacheService(PriceCacheRepository cacheRepository) {
        this.cacheRepository = cacheRepository;
    }

    public Optional<List<PriceResult>> getCachedResults(String query) {
        LocalDateTime expiryThreshold = LocalDateTime.now().minusHours(CACHE_EXPIRY_HOURS);
        List<PriceCache> cached = cacheRepository.findBySearchQueryIgnoreCaseAndCachedAtAfter(
                query.toLowerCase(), expiryThreshold);

        if (cached.isEmpty()) {
            return Optional.empty();
        }

        List<PriceResult> results = cached.stream()
                .map(this::toResult)
                .toList();

        log.debug("Cache hit for '{}': {} results", query, results.size());
        return Optional.of(results);
    }

    public boolean shouldRefreshInBackground(String query) {
        LocalDateTime refreshThreshold = LocalDateTime.now().minusHours(REFRESH_THRESHOLD_HOURS);
        List<PriceCache> cached = cacheRepository.findBySearchQueryIgnoreCaseAndCachedAtAfter(
                query.toLowerCase(), refreshThreshold);
        return cached.isEmpty();
    }

    @Transactional
    public void cacheResults(String query, List<PriceResult> results) {
        cacheRepository.deleteBySearchQueryIgnoreCase(query.toLowerCase());

        List<PriceCache> entities = results.stream()
                .map(r -> PriceCache.builder()
                        .searchQuery(query.toLowerCase())
                        .supermarketName(r.getSupermarketName())
                        .productName(r.getProductName())
                        .imageUrl(r.getImageUrl())
                        .logoUrl(r.getLogoUrl())
                        .address(r.getAddress())
                        .lat(r.getLat())
                        .lng(r.getLng())
                        .price(r.getPrice())
                        .unitPrice(r.getUnitPrice())
                        .cachedAt(LocalDateTime.now())
                        .build())
                .toList();

        cacheRepository.saveAll(entities);
        log.debug("Cached {} results for '{}'", entities.size(), query);
    }

    @Scheduled(fixedRate = 3600000) // Every hour
    @Transactional
    public void cleanupExpiredCache() {
        LocalDateTime expiry = LocalDateTime.now().minusHours(CACHE_EXPIRY_HOURS);
        cacheRepository.deleteExpiredEntries(expiry);
        log.info("Cleaned up expired cache entries older than {}", expiry);
    }

    private PriceResult toResult(PriceCache cache) {
        return PriceResult.builder()
                .productName(cache.getProductName())
                .imageUrl(cache.getImageUrl())
                .supermarketName(cache.getSupermarketName())
                .logoUrl(cache.getLogoUrl())
                .address(cache.getAddress())
                .lat(cache.getLat() != null ? cache.getLat() : 0)
                .lng(cache.getLng() != null ? cache.getLng() : 0)
                .price(cache.getPrice())
                .unitPrice(cache.getUnitPrice())
                .build();
    }
}
