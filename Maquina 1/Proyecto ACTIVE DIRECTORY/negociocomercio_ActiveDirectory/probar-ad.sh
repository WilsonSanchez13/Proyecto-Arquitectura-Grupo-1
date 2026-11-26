#!/bin/bash
# Uso: bash probar-ad.sh [URL] [CLAVE_API]
URL=${1:-http://localhost:8200}
CLAVE=${2:-clave-ad-2026}
USR="prueba$RANDOM"
CED="9$RANDOM$RANDOM"
J='Content-Type: application/json'
NUEVA='{"nombre":"Ana Prueba","cedula":"'$CED'","correo":"ana@ejemplo.com","celular":"3001234567","direccion":"Carrera 7 # 40-62, Bogotá","usuario":"'$USR'","contrasena":"Clave12345"}'

echo "1) estado (sin clave, debe dar 200):"
curl -s -w " -> %{http_code}\n" $URL/ad/estado
echo "2) sin X-Api-Key (debe dar 403):"
curl -s -w " -> %{http_code}\n" -X POST $URL/ad/autenticar -H "$J" -d '{"usuario":"cliente","contrasena":"1234"}'
echo "3) usuario de prueba (debe dar 200 con sus datos):"
curl -s -w " -> %{http_code}\n" -X POST $URL/ad/autenticar -H "$J" -H "X-Api-Key: $CLAVE" -d '{"usuario":"cliente","contrasena":"1234"}'
echo "4) clave incorrecta (debe dar 401):"
curl -s -w " -> %{http_code}\n" -X POST $URL/ad/autenticar -H "$J" -H "X-Api-Key: $CLAVE" -d '{"usuario":"cliente","contrasena":"mala"}'
echo "5) crear cuenta nueva (debe dar 201):"
curl -s -w " -> %{http_code}\n" -X POST $URL/ad/usuarios -H "$J" -H "X-Api-Key: $CLAVE" -d "$NUEVA"
echo "6) repetir la misma cuenta (debe dar 409):"
curl -s -w " -> %{http_code}\n" -X POST $URL/ad/usuarios -H "$J" -H "X-Api-Key: $CLAVE" -d "$NUEVA"
echo "7) entrar con la cuenta nueva (debe dar 200 con todos sus datos):"
curl -s -w " -> %{http_code}\n" -X POST $URL/ad/autenticar -H "$J" -H "X-Api-Key: $CLAVE" -d '{"usuario":"'$USR'","contrasena":"Clave12345"}'
echo "8) contraseña corta (debe dar 400 con mensaje):"
curl -s -w " -> %{http_code}\n" -X POST $URL/ad/usuarios -H "$J" -H "X-Api-Key: $CLAVE" -d '{"nombre":"X","cedula":"123456","correo":"x@x.com","celular":"3001234567","direccion":"Calle 1","usuario":"otro1","contrasena":"123"}'
