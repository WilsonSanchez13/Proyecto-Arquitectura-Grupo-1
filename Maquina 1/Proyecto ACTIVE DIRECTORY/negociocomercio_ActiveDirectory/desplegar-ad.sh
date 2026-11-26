#!/bin/bash
# Compila el directorio y lo instala como servicio fuera del IDE. Ejecutar desde esta carpeta.
set -e
cd "$(dirname "$0")"
./mvnw clean package -DskipTests
sudo mkdir -p /opt/activedirectory/data
sudo cp target/activedirectory.jar /opt/activedirectory/activedirectory.jar
sed "s/__USUARIO__/$USER/" activedirectory.service | sudo tee /etc/systemd/system/activedirectory.service > /dev/null
sudo chown -R "$USER" /opt/activedirectory
sudo systemctl daemon-reload
sudo systemctl enable activedirectory
sudo systemctl restart activedirectory
echo "Active Directory desplegado. Espera ~15 s y prueba: curl -s http://localhost:8200/ad/estado"
