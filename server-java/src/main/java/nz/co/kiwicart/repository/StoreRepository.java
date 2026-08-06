package nz.co.kiwicart.repository;

import nz.co.kiwicart.entity.Store;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface StoreRepository extends JpaRepository<Store, Long> {
}
