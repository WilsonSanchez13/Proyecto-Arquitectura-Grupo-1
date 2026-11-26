package com.negociocomercio.activedirectory;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

/** Datos que pide la pantalla «Crear cuenta». */
public record RegistroPeticion(
        @NotBlank(message = "El nombre es obligatorio.")
        @Size(max = 120, message = "El nombre no puede pasar de 120 caracteres.")
        String nombre,

        @NotBlank(message = "La cédula es obligatoria.")
        @Pattern(regexp = "\\d{6,12}", message = "La cédula debe tener entre 6 y 12 dígitos.")
        String cedula,

        @NotBlank(message = "El correo es obligatorio.")
        @Email(message = "El correo no tiene un formato válido.")
        @Size(max = 120, message = "El correo no puede pasar de 120 caracteres.")
        String correo,

        @NotBlank(message = "El celular es obligatorio.")
        @Pattern(regexp = "\\d{10}", message = "El celular debe tener 10 dígitos.")
        String celular,

        @NotBlank(message = "La dirección es obligatoria.")
        @Size(max = 200, message = "La dirección no puede pasar de 200 caracteres.")
        String direccion,

        @NotBlank(message = "El usuario es obligatorio.")
        @Pattern(regexp = "[A-Za-z0-9._-]{3,40}", message = "El usuario debe tener de 3 a 40 caracteres: letras, números, punto, guion o guion bajo.")
        String usuario,

        @NotBlank(message = "La contraseña es obligatoria.")
        @Size(min = 8, max = 72, message = "La contraseña debe tener entre 8 y 72 caracteres.")
        String contrasena) {
}
