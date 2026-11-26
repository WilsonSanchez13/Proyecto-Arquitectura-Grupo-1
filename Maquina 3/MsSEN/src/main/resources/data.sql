-- Registro semilla (id_compra=0 no colisiona con compras reales)
INSERT INTO sen_envios (id_compra, cedula, direccion, id_bien, descripcion, estado_envio)
VALUES (0, '0000', 'N/A', 0, 'REGISTRO SEMILLA', 'RECIBIDO');