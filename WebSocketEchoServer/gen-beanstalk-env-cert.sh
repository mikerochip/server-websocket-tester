#!/usr/bin/env bash
set -eo pipefail

# Setup prompted inputs
if [[ -z "$BEANSTALK_ENV_DOMAIN" ]]; then
    read -rp "Beanstalk Env Domain (set BEANSTALK_ENV_DOMAIN to skip this): " BEANSTALK_ENV_DOMAIN
    if [[ -z "$BEANSTALK_ENV_DOMAIN" ]]; then
        echo "Missing BEANSTALK_ENV_DOMAIN"
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
FriendlyName="websocket-tester"
Subject="/CN=websocket-tester"
SanEntries=(
    "DNS:localhost"
    "IP:127.0.0.1"
    "DNS:$BEANSTALK_ENV_DOMAIN"
)
San="$(IFS=,; echo "${SanEntries[*]}")"

# Use temp files for generating cert before converting to pfx
TempKeyFile="$(mktemp).key"
TempCertFile="$(mktemp).crt"
# cleanup even on error
cleanup() { rm -f "$TempKeyFile" "$TempCertFile"; }
trap cleanup EXIT

# Generate self-signed cert with SANs via -addext
openssl req -x509 -newkey rsa:2048 \
    -days 825 \
    -nodes \
    -keyout "$TempKeyFile" \
    -out "$TempCertFile" \
    -subj "$Subject" \
    -addext "subjectAltName=$San" \
    -addext "keyUsage=critical,digitalSignature,keyEncipherment" \
    -addext "extendedKeyUsage=serverAuth,clientAuth"

# Export to PFX
openssl pkcs12 -export \
    -inkey "$TempKeyFile" \
    -in "$TempCertFile" \
    -out "$PfxFileName" \
    -name "$FriendlyName" \
    -passout "pass:$PFX_PASSWORD"

echo "✅ Generated $PfxFileName"

echo ""

echo "Validate $PfxFileName:"
openssl pkcs12 -info -clcerts -nokeys \
    -in $PfxFileName \
    -passin "pass:$PFX_PASSWORD" | \
    openssl x509 -noout -text
