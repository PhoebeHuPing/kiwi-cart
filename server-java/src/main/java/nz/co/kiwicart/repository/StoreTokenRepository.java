package nz.co.kiwicart.repository;

import nz.co.kiwicart.entity.StoreToken;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface StoreTokenRepository extends JpaRepository<StoreToken, Long> {

    Optional<StoreToken> findByStoreBrand(String storeBrand);
}
