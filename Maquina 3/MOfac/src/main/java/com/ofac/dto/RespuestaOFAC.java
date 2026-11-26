package com.ofac.dto;

public class RespuestaOFAC {

    private String cedula;
    private String estado;
    private String mensaje;

    public RespuestaOFAC() {}

    public RespuestaOFAC(String cedula, String estado, String mensaje) {
        this.cedula = cedula;
        this.estado = estado;
        this.mensaje = mensaje;
    }

    public String getCedula() { return cedula; }
    public void setCedula(String cedula) { this.cedula = cedula; }

    public String getEstado() { return estado; }
    public void setEstado(String estado) { this.estado = estado; }

    public String getMensaje() { return mensaje; }
    public void setMensaje(String mensaje) { this.mensaje = mensaje; }
}