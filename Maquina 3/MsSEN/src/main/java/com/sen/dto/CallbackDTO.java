package com.sen.dto;

public class CallbackDTO {
    private Long idCompra;
    private String sistema;
    private Boolean ok;
    private String mensaje;

    public CallbackDTO() {}
    public CallbackDTO(Long idCompra, String sistema, Boolean ok, String mensaje) {
        this.idCompra = idCompra; this.sistema = sistema; this.ok = ok; this.mensaje = mensaje;
    }
    public Long getIdCompra() { return idCompra; }
    public void setIdCompra(Long idCompra) { this.idCompra = idCompra; }
    public String getSistema() { return sistema; }
    public void setSistema(String sistema) { this.sistema = sistema; }
    public Boolean getOk() { return ok; }
    public void setOk(Boolean ok) { this.ok = ok; }
    public String getMensaje() { return mensaje; }
    public void setMensaje(String mensaje) { this.mensaje = mensaje; }
}