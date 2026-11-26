package com.ofac.repository;

import com.ofac.entity.EntityOFAC;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface RepositoryOFAC extends JpaRepository<EntityOFAC, Long> {

    Optional<EntityOFAC> findByCedula(String cedula);

    List<EntityOFAC> findByEstado(String estado);
}
