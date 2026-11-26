package com.ofac.entity;

import jakarta.persistence.*;

@Entity
@Table(name = "ofac_consultas")
public class EntityOFAC {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, unique = true, length = 20)
    private String cedula;

    @Column(nullable = false, length = 20)
    private String estado; // APROBADO | RECHAZADO | ENPROCESO

    public EntityOFAC() {}

    public EntityOFAC(String cedula, String estado) {
        this.cedula = cedula;
        this.estado = estado;
    }

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }

    public String getCedula() { return cedula; }
    public void setCedula(String cedula) { this.cedula = cedula; }

    public String getEstado() { return estado; }
    public void setEstado(String estado) { this.estado = estado; }

    @Override
    public String toString() {
        return "EntityOFAC [id=" + id + ", cedula=" + cedula + ", estado=" + estado + "]";
    }
}