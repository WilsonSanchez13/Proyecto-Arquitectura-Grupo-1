package com.ofac.service;

import com.ofac.dto.RespuestaOFAC;
import com.ofac.entity.EntityOFAC;
import com.ofac.repository.RepositoryOFAC;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Optional;
import java.util.Random;

@RestController
@RequestMapping("/api/ofac")
public class ServiceOFAC {

    private final RepositoryOFAC repositoryOFAC;
    private final Random random = new Random();

    public ServiceOFAC(RepositoryOFAC repositoryOFAC) {
        this.repositoryOFAC = repositoryOFAC;
    }

    /**
     * Consulta el estado OFAC de una cédula.
     * - Si existe: devuelve el estado persistido.
     * - Si NO existe: simula consulta externa y persiste el resultado.
     */
    @GetMapping("/consultar/{cedula}")
    public RespuestaOFAC consultar(@PathVariable String cedula) {

        Optional<EntityOFAC> existente = repositoryOFAC.findByCedula(cedula);

        if (existente.isPresent()) {
            EntityOFAC e = existente.get();
            return new RespuestaOFAC(e.getCedula(), e.getEstado(), "Consulta desde BD");
        }

        // Cédula nueva -> simulación de consulta externa
        double r = random.nextDouble();
        String estado;
        if (r < 0.70) {
            estado = "APROBADO";
        } else if (r < 0.85) {
            estado = "RECHAZADO";
        } else {
            estado = "ENPROCESO";
        }

        EntityOFAC nuevo = new EntityOFAC(cedula, estado);
        repositoryOFAC.save(nuevo);

        return new RespuestaOFAC(cedula, estado, "Consulta simulada OFAC");
    }

    // ============================================================
    // ENDPOINTS DE LISTADO / DEBUG (para inspeccionar la BD)
    // ============================================================

    /**
     * Lista TODAS las consultas OFAC registradas.
     * GET http://localhost:8001/api/ofac/consultas
     */
    @GetMapping("/consultas")
    public List<EntityOFAC> listarTodas() {
        return repositoryOFAC.findAll();
    }

    /**
     * Obtiene una consulta por id.
     * GET http://localhost:8001/api/ofac/consultas/1
     */
    @GetMapping("/consultas/{id}")
    public ResponseEntity<EntityOFAC> obtenerPorId(@PathVariable Long id) {
        return repositoryOFAC.findById(id)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    /**
     * Obtiene el historial de una cédula.
     * GET http://localhost:8001/api/ofac/consultas/cedula/1001
     */
    @GetMapping("/consultas/cedula/{cedula}")
    public ResponseEntity<EntityOFAC> obtenerPorCedula(@PathVariable String cedula) {
        return repositoryOFAC.findByCedula(cedula)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    /**
     * Filtra consultas por estado.
     * GET http://localhost:8001/api/ofac/consultas/estado/ENPROCESO
     */
    
    @GetMapping("/consultas/estado/{estado}")
    public List<EntityOFAC> listarPorEstado(@PathVariable String estado) {
        return repositoryOFAC.findByEstado(estado);
    }

    // HEALTH
    @GetMapping("/health")
    public String health() {
        return "OFAC-UP";
    }
}