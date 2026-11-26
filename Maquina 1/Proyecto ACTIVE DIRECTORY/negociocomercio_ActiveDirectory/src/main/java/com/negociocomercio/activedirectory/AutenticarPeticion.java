package com.negociocomercio.activedirectory;

import jakarta.validation.constraints.NotBlank;

public record AutenticarPeticion(
        @NotBlank(message = "El usuario es obligatorio.") String usuario,
        @NotBlank(message = "La contraseña es obligatoria.") String contrasena) {
}
