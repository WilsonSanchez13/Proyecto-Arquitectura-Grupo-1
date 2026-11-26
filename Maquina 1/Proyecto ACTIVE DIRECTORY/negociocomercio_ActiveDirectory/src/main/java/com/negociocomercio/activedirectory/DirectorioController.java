package com.negociocomercio.activedirectory;

import jakarta.validation.Valid;
import java.util.Map;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/ad")
public class DirectorioController {

    private final DirectorioService servicio;

    public DirectorioController(DirectorioService servicio) {
        this.servicio = servicio;
    }

    /** Público: sirve para comprobar que el servicio está vivo. */
    @GetMapping("/estado")
    public Map<String, String> estado() {
        return Map.of("servicio", "activedirectory", "estado", "activo");
    }

    @PostMapping("/autenticar")
    public DatosUsuario autenticar(@Valid @RequestBody AutenticarPeticion p) {
        return servicio.autenticar(p.usuario(), p.contrasena());
    }

    @PostMapping("/usuarios")
    public ResponseEntity<Map<String, String>> crear(@Valid @RequestBody RegistroPeticion p) {
        servicio.crear(p);
        return ResponseEntity.status(HttpStatus.CREATED)
                .body(Map.of("mensaje", "Tu cuenta fue creada. Ya puedes ingresar."));
    }
}
