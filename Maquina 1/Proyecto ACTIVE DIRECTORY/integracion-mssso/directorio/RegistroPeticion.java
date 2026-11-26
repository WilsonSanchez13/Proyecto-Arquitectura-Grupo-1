package com.negociocomercio.mssso.directorio;

/** Mismo cuerpo que envía el front en POST /api/auth/registro. */
public record RegistroPeticion(String nombre, String cedula, String correo, String celular,
                               String direccion, String usuario, String contrasena) {
}
