package com.sin.repository;

import com.sin.entity.EntitySIN;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import java.util.List;

@Repository
public interface RepositorySIN extends JpaRepository<EntitySIN, Long> {
    List<EntitySIN> findByIdCompra(Long idCompra);
    boolean existsByIdCompraAndIdBien(Long idCompra, Long idBien);
}