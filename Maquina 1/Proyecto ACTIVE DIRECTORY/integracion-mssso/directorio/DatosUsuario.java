package com.negociocomercio.mssso.directorio;

/** Respuesta del directorio cuando las credenciales son correctas. */
public record DatosUsuario(String usuario, String nombre, String cedula, String correo, String celular, String direccion) {
}
