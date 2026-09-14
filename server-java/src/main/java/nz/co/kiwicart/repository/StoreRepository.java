package nz.co.kiwicart.repository;

import nz.co.kiwicart.entity.Store;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.Optional;

public interface StoreRepository extends JpaRepository<Store, Long> {

    List<Store> findByBrandContainingIgnoreCase(String brand);

    @Query("SELECT s FROM Store s WHERE LOWER(s.brand) LIKE LOWER(CONCAT('%', :brand, '%')) AND s.externalStoreId IS NOT NULL AND s.externalStoreId <> ''")
    List<Store> findByBrandWithExternalId(@Param("brand") String brand);

    Optional<Store> findByExternalStoreId(String externalStoreId);
}
