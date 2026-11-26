-- YoPago · datos de prueba. La API lo ejecuta al arrancar solo en desarrollo (BaseDatos:CargarDatosPrueba).


INSERT INTO directorio_activo (cedula, clave) VALUES ('1032456789', 'YoPago2026*')
ON CONFLICT (cedula) DO NOTHING;

INSERT INTO clientes (cedula, nombre, correo, intentos_fallidos, creado_en)
VALUES ('1032456789', 'Camilo Rojas', 'were06013@gmail.com', 0, now() - interval '60 days')
ON CONFLICT (cedula) DO UPDATE SET correo = EXCLUDED.correo; -- así el correo de prueba se actualiza en bases ya creadas

-- 1) Pendiente, por vencer en 2 días
INSERT INTO compras (numero_compra, cliente_id, descripcion, codigo_subasta, valor_oferta, impuestos, comision,
                     valor_total, recibida_en, vence_en, sincronizada_en, estado)
SELECT 'SSO-2026-08417', c.id, 'Lote 14 — Camioneta Jac T8 4x4, modelo 2023', 'Subasta 032',
       86500000, 4240000, 0, 90740000,
       now() - interval '4 days', now() + interval '2 days', now(), 'Pendiente'
FROM clientes c WHERE c.cedula = '1032456789'
ON CONFLICT (numero_compra) DO NOTHING;

-- 2) Pendiente
INSERT INTO compras (numero_compra, cliente_id, descripcion, codigo_subasta, valor_oferta, impuestos, comision,
                     valor_total, recibida_en, vence_en, sincronizada_en, estado)
SELECT 'SSO-2026-08402', c.id, 'Lote 07 — Bodega industrial 320 m², Funza', 'Subasta 031',
       312000000, 15900000, 0, 327900000,
       now() - interval '7 days', now() + interval '10 days', now(), 'Pendiente'
FROM clientes c WHERE c.cedula = '1032456789'
ON CONFLICT (numero_compra) DO NOTHING;

-- 3) Pagada (con su pago aprobado y acuse de SSO)
INSERT INTO compras (numero_compra, cliente_id, descripcion, codigo_subasta, valor_oferta, impuestos, comision,
                     valor_total, recibida_en, vence_en, pagada_en, sincronizada_en, estado)
SELECT 'SSO-2026-07988', c.id, 'Lote 02 — Apartamento 68 m², Cedritos', 'Subasta 027',
       204100000, 10200000, 0, 214300000,
       now() - interval '52 days', now() - interval '45 days', now() - interval '50 days', now(), 'Pagada'
FROM clientes c WHERE c.cedula = '1032456789'
ON CONFLICT (numero_compra) DO NOTHING;

INSERT INTO pagos (codigo_transaccion, compra_id, medio_pago, banco, tipo_cuenta, correo_comprobante, valor, estado,
                   instrumento, referencia_pasarela, codigo_acuse_sso, acuse_sso_en, creado_en, completado_en)
SELECT 'YP-87901145', co.id, 'Pse', 'Bancolombia', 'Ahorros', 'were06013@gmail.com', 214300000, 'Aprobado',
       'PSE · Bancolombia Ahorros *1145', 'SIM-YP-87901145', 'ACK-65210',
       now() - interval '50 days', now() - interval '50 days', now() - interval '50 days'
FROM compras co WHERE co.numero_compra = 'SSO-2026-07988'
ON CONFLICT (codigo_transaccion) DO NOTHING;
