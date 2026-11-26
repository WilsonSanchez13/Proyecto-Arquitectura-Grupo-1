-- Registro semilla (id_compra=0 no colisiona con compras reales)
INSERT INTO sin_bienes_vendidos (id_compra, cedula, id_bien, descripcion, cantidad, estado)
VALUES (0, '0000', 0, 'REGISTRO SEMILLA', 1, 'RECIBIDO');