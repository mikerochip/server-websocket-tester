#!/usr/bin/env bash
set -eo pipefail

ZipFileName="Beanstalk.zip"

echo "ℹ️ Delete \"$ZipFileName\"..."
rm -rf $ZipFileName

echo "ℹ️ Zip \"$(pwd)\" -> \"$ZipFileName\"..."
zip -r $ZipFileName . \
    -x "*.DS_Store" \
    -x "__MACOSX" \
    -x "bin/*" \
    -x "obj/*" \
    -x ".idea/*" \
    -x "*.sh"

echo "✅ Created \"$ZipFileName"
