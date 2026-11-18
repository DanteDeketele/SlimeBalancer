#!/usr/bin/env bash
# Packs the web game into a ZIP for the SlimeBalancer Duo site
# (Admin > Firmware updates > Publish a new release > Web game ZIP).
set -euo pipefail
cd "$(dirname "$0")"
version="${1:-1.0.0}"
mkdir -p dist
out="dist/slimebalancer-web-${version}.zip"
rm -f "$out"
zip -qr "$out" index.html css js assets -x '*.DS_Store'
echo "Wrote $out ($(du -h "$out" | cut -f1))"
