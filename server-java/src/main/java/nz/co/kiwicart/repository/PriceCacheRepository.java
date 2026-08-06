package nz.co.kiwicart.repository;

import nz.co.kiwicart.entity.PriceCache;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.List;

public interface PriceCacheRepository extends JpaRepository<PriceCache, Long> {

    List<PriceCache> findBySearchQueryIgnoreCaseAndCachedAtAfter(String searchQuery, LocalDateTime after);

    @Modifying
    @Transactional
    void deleteBySearchQueryIgnoreCase(String searchQuery);

    @Modifying
    @Transactional
    @Query("DELETE FROM PriceCache p WHERE p.cachedAt < :before")
    void deleteExpiredEntries(LocalDateTime before);
}
