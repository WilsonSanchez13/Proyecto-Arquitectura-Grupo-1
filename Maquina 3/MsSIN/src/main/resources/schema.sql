DROP TABLE IF EXISTS sin_bienes_vendidos;

CREATE TABLE sin_bienes_vendidos (
    id             BIGSERIAL PRIMARY KEY,
    id_compra      BIGINT       NOT NULL,
    cedula         VARCHAR(20)  NOT NULL,
    id_bien        BIGINT       NOT NULL,
    descripcion    VARCHAR(255),
    cantidad       INT          NOT NULL DEFAULT 1,
    estado         VARCHAR(30)  NOT NULL DEFAULT 'RECIBIDO',
    fecha_registro TIMESTAMP    NOT NULL DEFAULT NOW(),
    UNIQUE (id_compra, id_bien)
);

CREATE INDEX idx_sin_id_compra ON sin_bienes_vendidos (id_compra);