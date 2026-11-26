#!/bin/bash
# Compila el front en modo REAL (usarSimulado=false) y lo copia a NGINX.
set -e
cd ~/proyectos/ssofront
sed -i 's/usarSimulado: true/usarSimulado: false/' src/environments/environment.ts
grep usarSimulado src/environments/environment.ts
npx ng build
sudo mkdir -p /var/www/ssofront
sudo rm -rf /var/www/ssofront/*
sudo cp -r dist/ssofront/browser/* /var/www/ssofront/
echo "Front desplegado en modo REAL (usa el backend de la M2)."
