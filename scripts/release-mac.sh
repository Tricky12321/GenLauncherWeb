#!/usr/bin/env bash
# Build release artifacts for macOS (DMG — x64 and arm64).
# MUST be run on macOS. Requires: electronize, dotnet, node, yarn.
#
# Usage:
#   ./scripts/release-mac.sh <version>
#   ./scripts/release-mac.sh 1.2.0

set -euo pipefail

VERSION="${1:?Usage: $0 <version>  e.g.  $0 1.2.0}"

# ------------------------------------------------------------------ guard
if [[ "$(uname)" != "Darwin" ]]; then
    echo "ERROR: This script must be run on macOS."
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../GenlauncherWeb"
OUTPUT_DIR="$SCRIPT_DIR/../bin/Desktop"

# ------------------------------------------------------------------ helpers
check_cmd() {
    if ! command -v "$1" &>/dev/null; then
        echo "ERROR: '$1' not found on PATH."
        if [ -n "${2:-}" ]; then echo "  $2"; fi
        exit 1
    fi
}

# ------------------------------------------------------------------ prerequisites
echo "==> Checking prerequisites..."
check_cmd electronize "Install with: dotnet tool install ElectronNET.CLI -g"
check_cmd dotnet      "Install from: https://dotnet.microsoft.com/download"
check_cmd node        "Install from: https://nodejs.org"
check_cmd yarn        "Install with: npm install -g yarn"

# ------------------------------------------------------------------ build
cd "$PROJECT_DIR"

echo ""
echo "==> Installing frontend dependencies..."
(cd ClientApp && yarn install --frozen-lockfile)

echo ""
echo "========================================"
echo " Building macOS (DMG x64) v${VERSION}"
echo "========================================"
electronize build /target osx /electron-arch x64 /Version "$VERSION"

echo ""
echo "========================================"
echo " Building macOS (DMG arm64) v${VERSION}"
echo "========================================"
electronize build /target osx /electron-arch arm64 /Version "$VERSION"

# ------------------------------------------------------------------ report
echo ""
echo "========================================"
echo " Release artifacts"
echo "========================================"
ls -lh "$OUTPUT_DIR"/*.dmg 2>/dev/null \
    || ls -lh "$OUTPUT_DIR"
echo ""
echo "Done. Upload the .dmg files to the GitHub release."
