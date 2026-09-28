#!/usr/bin/env bash
# ── 本地打包 · Local packaging ──
# 构建 Release 并把 dll 按游戏根目录结构打成 zip
# Build Release and zip the dll using the game-root layout
# 用法 / Usage：scripts/package.sh
# 前置 / Prerequisite：本机已安装 SPT 客户端（用 SPT_DIR 指定其根目录）
#   A local SPT client install (point SPT_DIR at its root)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

# 从 csproj 读取版本号 / Read the version from the csproj
VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj | head -1)"
[ -n "$VERSION" ] || { echo "错误：未能从 csproj 读取 Version | Error: cannot read Version from csproj" >&2; exit 1; }

OUT="artifacts"
STAGE="$OUT/PerformanceScope"

# 清理旧产物 / Clean previous output
rm -rf "$OUT"
mkdir -p "$STAGE/BepInEx/plugins/PerformanceScope"

# 构建但不自动部署，改由 zip 分发 / Build without auto-deploy; the zip is the deliverable
dotnet build src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj -c Release -p:DeployToGame=false

cp "src/PerformanceScope.Plugin/bin/Release/PerformanceScope.dll" \
   "$STAGE/BepInEx/plugins/PerformanceScope/"

(
  cd "$STAGE"
  zip -qr "../PerformanceScope-v${VERSION}.zip" .
)
rm -rf "$STAGE"

echo "已生成 artifacts/PerformanceScope-v${VERSION}.zip | Generated artifacts/PerformanceScope-v${VERSION}.zip"
