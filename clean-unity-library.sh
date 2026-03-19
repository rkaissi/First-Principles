#!/usr/bin/env bash
# Restore Unity packages: remove Library (PackageCache) and any broken embedded
# copies of registry packages under Packages/com.unity.* that should NOT exist
# for this project (only manifest.json + packages-lock.json are in git).
#
# Usage from repo root: ./clean-unity-library.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
PROJ="$ROOT/First Principles"
if [[ ! -d "$PROJ" ]]; then
  echo "Expected Unity project at: $PROJ"
  exit 1
fi

# Embedded registry packages that commonly get half-copied and break compile
# (missing UIToolkitInteroperabilityBridge, NUnitExtensions, orphan .meta, etc.)
for name in com.unity.ugui com.unity.test-framework; do
  dir="$PROJ/Packages/$name"
  if [[ -d "$dir" ]]; then
    echo "Removing embedded package folder (will resolve from registry): $dir"
    rm -rf "$dir"
  fi
done

# Warn if other com.unity.* folders exist under Packages (unexpected for this repo)
shopt -s nullglob
for dir in "$PROJ/Packages"/com.unity.*; do
  [[ -d "$dir" ]] || continue
  echo "WARNING: Unexpected folder remains under Packages/: $dir"
  echo "  If you did not embed this on purpose, delete it and reopen Unity."
done
shopt -u nullglob

LIB="$PROJ/Library"
if [[ -d "$LIB" ]]; then
  echo "Removing: $LIB"
  rm -rf "$LIB"
else
  echo "No Library folder at $LIB (skipped)."
fi

echo ""
echo "Done. Quit Unity if it was open, then reopen the project and wait for package restore."
