#!/usr/bin/env bash
set -eo pipefail

# Setup prompted inputs
if [[ -z "$DOMAIN" ]]; then
    read -rp "Cert Domain (set DOMAIN to skip this): " DOMAIN
    if [[ -z "$DOMAIN" ]]; then
        echo "Missing DOMAIN"
        exit 1
    fi
fi

if [[ -z "$PFX_PASSWORD" ]]; then
    read -s -p "Enter PFX password: " PFX_PASSWORD
    echo
    read -s -p "Confirm PFX password: " PfxPasswordConfirm
    echo
    if [[ "$PFX_PASSWORD" != "$PfxPasswordConfirm" ]]; then
        echo "Passwords do not match"
        exit 1
    fi
fi

# Setup inputs
PfxFileName="websocket-tester.pfx"
Subject="/CN=websocket-tester"
SanEntries=(
    "DNS:localhost"
    "IP:127.0.0.1"
    "DNS:$DOMAIN"
)
San="$(IFS=,; echo "${SanEntries[*]}")"

# Use temp files for generating cert before converting to pfx
TempCertFile="$(mktemp).pem"
TempKeyFile="$(mktemp).pem"
# cleanup even on error
cleanup() { rm -f "$TempKeyFile" "$TempCertFile"; }
trap cleanup EXIT

# Generate self-signed cert with SANs via -addext
echo "ℹ️ Generate cert..."
openssl req -x509 \
    -newkey rsa:2048 \
    -days 825 \
    -nodes \
    -out "$TempCertFile" \
    -keyout "$TempKeyFile" \
    -subj "$Subject" \
    -addext "subjectAltName=$San"

# Export to PFX
echo "ℹ️ Export pfx..."
openssl pkcs12 -export \
    -macalg SHA1 -certpbe PBE-SHA1-3DES -keypbe PBE-SHA1-3DES \
    -inkey "$TempKeyFile" \
    -in "$TempCertFile" \
    -out "$PfxFileName" \
    -password "pass:$PFX_PASSWORD"

echo "✅ Generated $PfxFileName"

echo ""

echo "Dump \"$PfxFileName\"..."
openssl pkcs12 -info -clcerts -nokeys -in $PfxFileName -passin "pass:$PFX_PASSWORD" \
    | openssl x509 -noout -text

echo "✅ Dumped $PfxFileName"
