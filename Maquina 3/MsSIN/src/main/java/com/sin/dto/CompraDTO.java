package com.sin.dto;

import java.util.List;

public class CompraDTO {
    private Long idCompra;
    private String cedula;
    private Double valorPagado;
    private List<BienDTO> bienes;

    public Long getIdCompra() { return idCompra; }
    public void setIdCompra(Long idCompra) { this.idCompra = idCompra; }
    public String getCedula() { return cedula; }
    public void setCedula(String cedula) { this.cedula = cedula; }
    public Double getValorPagado() { return valorPagado; }
    public void setValorPagado(Double valorPagado) { this.valorPagado = valorPagado; }
    public List<BienDTO> getBienes() { return bienes; }
    public void setBienes(List<BienDTO> bienes) { this.bienes = bienes; }

    public static class BienDTO {
        private Long idBien;
        private String descripcion;
        private Integer cantidad;
        public Long getIdBien() { return idBien; }
        public void setIdBien(Long idBien) { this.idBien = idBien; }
        public String getDescripcion() { return descripcion; }
        public void setDescripcion(String descripcion) { this.descripcion = descripcion; }
        public Integer getCantidad() { return cantidad; }
        public void setCantidad(Integer cantidad) { this.cantidad = cantidad; }
    }
}
