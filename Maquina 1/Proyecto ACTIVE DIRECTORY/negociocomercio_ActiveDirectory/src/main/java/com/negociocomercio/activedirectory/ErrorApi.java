package com.negociocomercio.activedirectory;

import org.springframework.http.HttpStatus;

/** Error con código HTTP y el texto que verá el cliente en {"mensaje": ...}. */
public class ErrorApi extends RuntimeException {
    private final HttpStatus estado;

    public ErrorApi(HttpStatus estado, String mensaje) {
        super(mensaje);
        this.estado = estado;
    }

    public HttpStatus getEstado() { return estado; }
}
