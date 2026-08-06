package nz.co.kiwicart.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Entity
@Table(name = "stores")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Store {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String name;

    private String brand;

    @Column(name = "logo_url")
    private String logoUrl;

    private String address;

    private Double latitude;

    private Double longitude;

    @Column(name = "external_id")
    private String externalId;
}
