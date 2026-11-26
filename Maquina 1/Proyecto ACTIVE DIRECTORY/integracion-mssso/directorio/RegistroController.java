package com.negociocomercio.mssso.directorio;

import java.util.Map;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** POST /api/auth/registro: el monolito delega la creación de la cuenta en el Active Directory. */
@RestController
@RequestMapping("/api/auth")
public class RegistroController {

    private final DirectorioActivoClient directorio;

    public RegistroController(DirectorioActivoClient directorio) {
        this.directorio = directorio;
    }

    @PostMapping("/registro")
    public ResponseEntity<Map<String, String>> registrar(@RequestBody RegistroPeticion p) {
        try {
            directorio.registrar(p);
            return ResponseEntity.status(201).body(Map.of("mensaje", "Tu cuenta fue creada. Ya puedes ingresar."));
        } catch (DirectorioRechazoException e) {
            return ResponseEntity.status(e.getEstado()).body(Map.of("mensaje", e.getMessage()));
        } catch (DirectorioNoDisponibleException e) {
            return ResponseEntity.status(503).body(Map.of("mensaje", e.getMessage()));
        }
    }
}
