package nz.co.kiwicart.config;

import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.cache.concurrent.ConcurrentMapCache;
import org.springframework.cache.support.SimpleCacheManager;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.scheduling.annotation.Scheduled;

import java.util.List;

@Configuration
@EnableAsync
@EnableScheduling
@EnableCaching
public class AsyncConfig {

    @Bean
    public CacheManager cacheManager() {
        SimpleCacheManager cacheManager = new SimpleCacheManager();
        cacheManager.setCaches(List.of(
                new ConcurrentMapCache("compareResults"),
                new ConcurrentMapCache("supermarkets")
        ));
        return cacheManager;
    }

    /**
     * Evict compare cache every 5 minutes (300 seconds) to keep data fresh.
     */
    @Scheduled(fixedRate = 300_000)
    public void evictCompareCache() {
        var cache = cacheManager().getCache("compareResults");
        if (cache != null) cache.clear();
    }

    /**
     * Evict supermarkets cache every 10 minutes (600 seconds).
     */
    @Scheduled(fixedRate = 600_000)
    public void evictSupermarketsCache() {
        var cache = cacheManager().getCache("supermarkets");
        if (cache != null) cache.clear();
    }
}
