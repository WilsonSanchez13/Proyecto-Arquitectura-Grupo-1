#!/bin/bash
# Estado de la M1: NGINX, ApiGateway, puertos y alcance de la M2.
echo "=== Servicios ==="
echo "nginx:      $(systemctl is-active nginx)"
echo "apigateway: $(systemctl is-active apigateway)"
echo
echo "=== Puertos en escucha (8080 publico, 8090 interno, 8000 gateway) ==="
ss -ltn | grep -E ':8080|:8090|:8000'
echo
echo "=== Gateway ==="
curl -s -m 3 http://localhost:8000/gateway/estado
echo
echo
echo "=== Front por NGINX ==="
curl -s -m 3 -o /dev/null -w "HTTP %{http_code}\n" http://localhost:8080/
echo
echo "=== Alcance de la M2 (10.43.98.252) ==="
for p in 8000 8080; do
  nc -zv -w 3 10.43.98.252 $p 2>&1
done
