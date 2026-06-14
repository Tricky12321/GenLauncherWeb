#!/usr/bin/env bash
# Build release artifacts for Windows (NSIS installer) and Linux (Flatpak).
# Run from any directory. Requires: electronize, dotnet, node, yarn, flatpak-builder.
#
# Usage:
#   ./scripts/release-win-linux.sh <version>
#   ./scripts/release-win-linux.sh 1.2.0

set -euo pipefail

VERSION="${1:?Usage: $0 <version>  e.g.  $0 1.2.0}"

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
check_cmd electronize  "Install with: dotnet tool install ElectronNET.CLI -g"
check_cmd dotnet       "Install from: https://dotnet.microsoft.com/download"
check_cmd node         "Install from: https://nodejs.org"
check_cmd yarn         "Install with: npm install -g yarn"
check_cmd flatpak      "Install with: sudo apt install flatpak  OR  sudo pacman -S flatpak"
check_cmd flatpak-builder "Install with: sudo apt install flatpak-builder  OR  sudo pacman -S flatpak-builder"

# Ensure Flathub is registered
if ! flatpak remotes 2>/dev/null | grep -q flathub; then
    echo "==> Adding Flathub remote..."
    flatpak remote-add --if-not-exists --user flathub \
        https://flathub.org/repo/flathub.flatpakrepo
fi

# Freedesktop Platform runtime required by electron-builder flatpak
RUNTIME_VERSION="24.08"
if ! flatpak list --runtime --columns=ref 2>/dev/null \
        | grep -q "org.freedesktop.Platform//${RUNTIME_VERSION}"; then
    echo "==> Installing Freedesktop Platform runtime ${RUNTIME_VERSION}..."
    flatpak install --user -y flathub \
        "org.freedesktop.Platform//${RUNTIME_VERSION}" \
        "org.freedesktop.Sdk//${RUNTIME_VERSION}"
fi

# ------------------------------------------------------------------ build
cd "$PROJECT_DIR"

echo ""
echo "==> Installing frontend dependencies..."
(cd ClientApp && yarn install --frozen-lockfile)

echo ""
echo "========================================"
echo " Building Windows (NSIS installer) v${VERSION}"
echo "========================================"
electronize build /target win /Version "$VERSION"

echo ""
echo "========================================"
echo " Building Linux (Flatpak) v${VERSION}"
echo "========================================"
electronize build /target linux /Version "$VERSION"

# ------------------------------------------------------------------ report
echo ""
echo "========================================"
echo " Release artifacts"
echo "========================================"
ls -lh "$OUTPUT_DIR"/*.exe "$OUTPUT_DIR"/*.flatpak 2>/dev/null \
    || ls -lh "$OUTPUT_DIR"
echo ""
echo "Done. Upload the .exe and .flatpak files to the GitHub release."
