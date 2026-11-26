DROP TABLE IF EXISTS sen_envios;

CREATE TABLE sen_envios (
    id             BIGSERIAL PRIMARY KEY,
    id_compra      BIGINT       NOT NULL,
    cedula         VARCHAR(20)  NOT NULL,
    direccion      VARCHAR(255) NOT NULL,
    id_bien        BIGINT       NOT NULL,
    descripcion    VARCHAR(255),
    estado_envio   VARCHAR(30)  NOT NULL DEFAULT 'RECIBIDO',
    fecha_registro TIMESTAMP    NOT NULL DEFAULT NOW(),
    UNIQUE (id_compra, id_bien)
);

CREATE INDEX idx_sen_id_compra ON sen_envios (id_compra);