#!/usr/bin/env bash
set -eo pipefail

ZipFileName="Beanstalk.zip"

rm -rf $ZipFileName
zip -r $ZipFileName . \
    -x "*.DS_Store" \
    -x "__MACOSX" \
    -x "bin/*" \
    -x "obj/*" \
    -x ".idea/*" \
    -x "*.sh"
