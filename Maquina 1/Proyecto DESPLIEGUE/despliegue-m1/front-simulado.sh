#!/bin/bash
# Compila el front en modo SIMULADO (usarSimulado=true) y lo copia a NGINX.
set -e
cd ~/proyectos/ssofront
sed -i 's/usarSimulado: false/usarSimulado: true/' src/environments/environment.ts
grep usarSimulado src/environments/environment.ts
npx ng build
sudo mkdir -p /var/www/ssofront
sudo rm -rf /var/www/ssofront/*
sudo cp -r dist/ssofront/browser/* /var/www/ssofront/
echo "Front desplegado en modo SIMULADO (no necesita la M2)."
