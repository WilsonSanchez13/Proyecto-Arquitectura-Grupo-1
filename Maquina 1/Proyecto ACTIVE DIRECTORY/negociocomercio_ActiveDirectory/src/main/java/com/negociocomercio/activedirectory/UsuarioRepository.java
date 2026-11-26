package com.negociocomercio.activedirectory;

import java.util.Optional;
import org.springframework.data.jpa.repository.JpaRepository;

public interface UsuarioRepository extends JpaRepository<UsuarioDirectorio, Long> {
    Optional<UsuarioDirectorio> findByUsuario(String usuario);
    boolean existsByUsuario(String usuario);
    boolean existsByCedula(String cedula);
}
