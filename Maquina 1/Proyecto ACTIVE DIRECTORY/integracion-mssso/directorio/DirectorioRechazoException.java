package com.negociocomercio.mssso.directorio;

/** El directorio respondió con un 4xx (credenciales incorrectas, datos inválidos, usuario repetido...). */
public class DirectorioRechazoException extends RuntimeException {
    private final int estado;

    public DirectorioRechazoException(int estado, String mensaje) {
        super(mensaje);
        this.estado = estado;
    }

    public int getEstado() { return estado; }
}
