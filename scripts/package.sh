#!/usr/bin/env bash
# 本地打包：构建 Release 并把 dll 按游戏根目录结构打成 zip
# 用法：scripts/package.sh
# 前置：本机已安装 SPT 客户端（用 SPT_DIR 指定其根目录）
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj | head -1)"
[ -n "$VERSION" ] || { echo "错误：未能从 csproj 读取 Version" >&2; exit 1; }

OUT="artifacts"
STAGE="$OUT/PerformanceScope"

rm -rf "$OUT"
mkdir -p "$STAGE/BepInEx/plugins/PerformanceScope"

dotnet build src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj -c Release -p:DeployToGame=false

cp "src/PerformanceScope.Plugin/bin/Release/PerformanceScope.dll" \
   "$STAGE/BepInEx/plugins/PerformanceScope/"

(
  cd "$STAGE"
  zip -qr "../PerformanceScope-v${VERSION}.zip" .
)
rm -rf "$STAGE"

echo "已生成 artifacts/PerformanceScope-v${VERSION}.zip"
