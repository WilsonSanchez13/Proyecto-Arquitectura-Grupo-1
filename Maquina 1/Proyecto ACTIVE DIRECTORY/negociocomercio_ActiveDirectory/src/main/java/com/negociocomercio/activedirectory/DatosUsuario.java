package com.negociocomercio.activedirectory;

/** Lo que el directorio devuelve cuando las credenciales son correctas. Nunca incluye la clave. */
public record DatosUsuario(String usuario, String nombre, String cedula, String correo, String celular, String direccion) {

    static DatosUsuario de(UsuarioDirectorio u) {
        return new DatosUsuario(u.getUsuario(), u.getNombre(), u.getCedula(), u.getCorreo(), u.getCelular(), u.getDireccion());
    }
}
