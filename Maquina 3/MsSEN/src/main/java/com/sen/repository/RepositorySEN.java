package com.sen.repository;

import com.sen.entity.EntitySEN;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import java.util.List;

@Repository
public interface RepositorySEN extends JpaRepository<EntitySEN, Long> {
    List<EntitySEN> findByIdCompra(Long idCompra);
    boolean existsByIdCompraAndIdBien(Long idCompra, Long idBien);
}