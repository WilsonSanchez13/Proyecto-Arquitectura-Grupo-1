DROP TABLE IF EXISTS ofac_consultas;

CREATE TABLE ofac_consultas (
    id      BIGSERIAL PRIMARY KEY,
    cedula  VARCHAR(20) NOT NULL UNIQUE,
    estado  VARCHAR(20) NOT NULL
            CHECK (estado IN ('APROBADO', 'RECHAZADO', 'ENPROCESO'))
);

CREATE INDEX idx_ofac_cedula ON ofac_consultas (cedula);