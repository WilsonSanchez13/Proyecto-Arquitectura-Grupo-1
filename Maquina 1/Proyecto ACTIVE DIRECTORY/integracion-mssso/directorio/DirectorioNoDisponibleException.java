package com.negociocomercio.mssso.directorio;

/** El directorio no responde (red caída, servicio apagado o clave de API mal configurada). */
public class DirectorioNoDisponibleException extends RuntimeException {
    public DirectorioNoDisponibleException() {
        super("El directorio de usuarios no está disponible en este momento. Intenta de nuevo en unos minutos.");
    }
}
