package nz.co.kiwicart.repository;

import nz.co.kiwicart.entity.Product;
import org.springframework.data.jpa.repository.JpaRepository;

public interface ProductRepository extends JpaRepository<Product, Long> {
}
