package nz.co.kiwicart.repository;

import nz.co.kiwicart.entity.Favorite;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface FavoriteRepository extends JpaRepository<Favorite, Long> {

    List<Favorite> findByUserIdOrderByCreatedAtDesc(String userId);

    Optional<Favorite> findByUserIdAndProductName(String userId, String productName);

    void deleteByUserIdAndProductName(String userId, String productName);
}
