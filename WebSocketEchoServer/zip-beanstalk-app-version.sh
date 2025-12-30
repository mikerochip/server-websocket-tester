#!/usr/bin/env bash
set -eo pipefail

zip -r Beanstalk.zip . \
    -x "*.DS_Store" \
    -x "__MACOSX" \
    -x "bin/*" \
    -x "obj/*" \
    -x ".idea/*"
