#!/bin/bash
set -e  # Detener si hay error

echo "🔐 Configuración de Certificados OpenIddict para Acme"
echo "=================================================="

# Configuración
CERT_PASSWORD="Acme2025!SecurePassword$(openssl rand -base64 12)"
CERT_DIR="/var/acme/certs"
BACKUP_DIR="/var/acme/backups"
APP_USER="${SUDO_USER:-$USER}"

echo ""
echo "📦 Paso 1: Verificando OpenSSL..."
if ! command -v openssl &> /dev/null; then
    echo "⚙️  Instalando OpenSSL..."
    if [ -f /etc/debian_version ]; then
        sudo apt-get update -qq
        sudo apt-get install -y openssl
    elif [ -f /etc/redhat-release ]; then
        sudo yum install -y openssl
    else
        echo "❌ Sistema operativo no soportado. Instala OpenSSL manualmente."
        exit 1
    fi
fi
echo "✅ OpenSSL instalado: $(openssl version)"

echo ""
echo "📁 Paso 2: Creando directorios..."
sudo mkdir -p "$CERT_DIR"
sudo mkdir -p "$BACKUP_DIR"
mkdir -p ~/acme-certs-temp
cd ~/acme-certs-temp

echo ""
echo "🔑 Paso 3: Generando Certificado de Firma (Signing)..."
openssl req -x509 -newkey rsa:4096 \
  -keyout signing-key.pem \
  -out signing-cert.pem \
  -days 3650 \
  -nodes \
  -subj "/CN=Acme Signing Certificate/O=Acme/C=US" \
  2>/dev/null

openssl pkcs12 -export \
  -out signing-cert.pfx \
  -inkey signing-key.pem \
  -in signing-cert.pem \
  -password pass:"$CERT_PASSWORD" \
  2>/dev/null

echo "✅ Certificado de firma generado"

echo ""
echo "🔐 Paso 4: Generando Certificado de Encriptación (Encryption)..."
openssl req -x509 -newkey rsa:4096 \
  -keyout encryption-key.pem \
  -out encryption-cert.pem \
  -days 3650 \
  -nodes \
  -subj "/CN=Acme Encryption Certificate/O=Acme/C=US" \
  2>/dev/null

openssl pkcs12 -export \
  -out encryption-cert.pfx \
  -inkey encryption-key.pem \
  -in encryption-cert.pem \
  -password pass:"$CERT_PASSWORD" \
  2>/dev/null

echo "✅ Certificado de encriptación generado"

echo ""
echo "📦 Paso 5: Instalando certificados..."
sudo mv signing-cert.pfx "$CERT_DIR/"
sudo mv encryption-cert.pfx "$CERT_DIR/"
sudo chmod 600 "$CERT_DIR"/*.pfx
sudo chown "$APP_USER:$APP_USER" "$CERT_DIR"/*.pfx

echo ""
echo "💾 Paso 6: Creando backup..."
BACKUP_FILE="$BACKUP_DIR/certs-backup-$(date +%Y%m%d-%H%M%S).tar.gz"
sudo tar -czf "$BACKUP_FILE" -C "$CERT_DIR" .
sudo chmod 600 "$BACKUP_FILE"
sudo chown "$APP_USER:$APP_USER" "$BACKUP_FILE"
echo "✅ Backup creado: $BACKUP_FILE"

echo ""
echo "🧹 Paso 7: Limpiando archivos temporales..."
cd ~
rm -rf ~/acme-certs-temp
echo "✅ Limpieza completada"

echo ""
echo "📝 Paso 8: Creando archivo de configuración..."
CONFIG_FILE="$CERT_DIR/config.env"
sudo tee "$CONFIG_FILE" > /dev/null <<EOF
# OpenIddict Certificate Configuration
# Generated: $(date)
# IMPORTANTE: Guarda este archivo de forma segura

export OpenIddict__EncryptionCertificatePath="$CERT_DIR/encryption-cert.pfx"
export OpenIddict__SigningCertificatePath="$CERT_DIR/signing-cert.pfx"
export OpenIddict__CertificatePassword="$CERT_PASSWORD"
EOF

sudo chmod 600 "$CONFIG_FILE"
sudo chown "$APP_USER:$APP_USER" "$CONFIG_FILE"

echo ""
echo "=================================================="
echo "✅ ¡INSTALACIÓN COMPLETADA!"
echo "=================================================="
echo ""
echo "📍 Ubicación de certificados: $CERT_DIR"
echo "📍 Backup guardado en: $BACKUP_FILE"
echo "📍 Configuración: $CONFIG_FILE"
echo ""
echo "🔑 Contraseña generada (GUÁRDALA):"
echo "   $CERT_PASSWORD"
echo ""
echo "📋 PRÓXIMOS PASOS:"
echo ""
echo "1️⃣  Agregar variables de entorno a tu aplicación:"
echo "   source $CONFIG_FILE"
echo ""
echo "2️⃣  Si usas systemd, agrega a tu service file:"
echo "   EnvironmentFile=$CONFIG_FILE"
echo ""
echo "3️⃣  Si usas Docker, agrega a docker-compose.yml:"
echo "   env_file:"
echo "     - $CONFIG_FILE"
echo ""
echo "4️⃣  Verificar certificados:"
echo "   ls -lh $CERT_DIR"
echo ""
echo "⚠️  IMPORTANTE: Guarda la contraseña en un lugar seguro"
echo "⚠️  Haz backup del archivo: $BACKUP_FILE"
echo ""
