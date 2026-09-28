#!/usr/bin/env bash
# 本地一条命令发布：构建 → 打包 → 打 tag → 推送 → 创建 GitHub Release
# 用法：scripts/release.sh [--dry-run]
# 前置：已安装并登录 gh，且仓库已配置 remote origin
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

DRY_RUN=0
if [ "${1:-}" = "--dry-run" ]; then
  DRY_RUN=1
fi

VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj | head -1)"
[ -n "$VERSION" ] || { echo "错误：未能从 csproj 读取 Version" >&2; exit 1; }
TAG="v$VERSION"

# 工作区必须干净，避免发布内容与提交不一致
if [ -n "$(git status --porcelain)" ]; then
  echo "错误：工作区有未提交改动，请先提交" >&2
  git status --short
  exit 1
fi

BRANCH="$(git rev-parse --abbrev-ref HEAD)"
if [ "$BRANCH" = "HEAD" ]; then
  echo "错误：处于游离 HEAD，无法发布" >&2
  exit 1
fi

if git rev-parse -q --verify "refs/tags/$TAG" >/dev/null; then
  echo "错误：tag $TAG 已存在" >&2
  exit 1
fi

echo "版本：$VERSION   标签：$TAG   分支：$BRANCH"

# 构建并打包（--dry-run 也会执行，以校验构建与产物）
bash scripts/package.sh
ZIP="artifacts/PerformanceScope-v${VERSION}.zip"
[ -f "$ZIP" ] || { echo "错误：未找到产物 $ZIP" >&2; exit 1; }

if [ "$DRY_RUN" = "1" ]; then
  echo "[dry-run] 将执行："
  echo "  git tag -a $TAG -m \"$TAG\""
  echo "  git push origin $BRANCH"
  echo "  git push origin $TAG"
  echo "  gh release create $TAG $ZIP --generate-notes"
  exit 0
fi

command -v gh >/dev/null 2>&1 || { echo "错误：未安装 GitHub CLI（gh）" >&2; exit 1; }
git remote get-url origin >/dev/null 2>&1 || { echo "错误：未配置 git remote origin" >&2; exit 1; }

git tag -a "$TAG" -m "PerformanceScope $TAG"
git push origin "$BRANCH"
git push origin "$TAG"
gh release create "$TAG" "$ZIP" --title "PerformanceScope $TAG" --generate-notes

echo "已发布 $TAG（产物：$ZIP）"
