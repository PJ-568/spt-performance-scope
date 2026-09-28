#!/usr/bin/env bash
# ── 本地一键发布 · One-command local release ──
# 构建 → 打包 → 打 tag → 推送 → 创建 GitHub Release
# build → package → tag → push → create GitHub Release
# 用法 / Usage：scripts/release.sh [--dry-run]
# 前置 / Prerequisites：已安装 gh 并完成鉴权（gh auth login，或提供 GH_TOKEN），且仓库已配置 remote origin
#   GitHub CLI (gh) installed and authenticated (gh auth login, or provide GH_TOKEN), with a configured remote origin
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

DRY_RUN=0
if [ "${1:-}" = "--dry-run" ]; then
  DRY_RUN=1
fi

# 从 csproj 读取版本号 / Read the version from the csproj
VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj | head -1)"
[ -n "$VERSION" ] || { echo "错误：未能从 csproj 读取 Version | Error: cannot read Version from csproj" >&2; exit 1; }
TAG="v$VERSION"

# 工作区必须干净，避免发布内容与提交不一致
# Require a clean work tree so the release matches the commit
if [ -n "$(git status --porcelain)" ]; then
  echo "错误：工作区有未提交改动，请先提交 | Error: uncommitted changes; commit them first" >&2
  git status --short
  exit 1
fi

BRANCH="$(git rev-parse --abbrev-ref HEAD)"
if [ "$BRANCH" = "HEAD" ]; then
  echo "错误：处于游离 HEAD，无法发布 | Error: detached HEAD, cannot release" >&2
  exit 1
fi

# tag 不能已存在 / The tag must not already exist
if git rev-parse -q --verify "refs/tags/$TAG" >/dev/null; then
  echo "错误：tag $TAG 已存在 | Error: tag $TAG already exists" >&2
  exit 1
fi

echo "版本 / Version：$VERSION   标签 / Tag：$TAG   分支 / Branch：$BRANCH"

# 构建并打包（--dry-run 也会执行，以校验构建与产物）
# Build and package (also runs under --dry-run to validate the build and artifact)
bash scripts/package.sh
ZIP="artifacts/PerformanceScope-v${VERSION}.zip"
[ -f "$ZIP" ] || { echo "错误：未找到产物 $ZIP | Error: artifact not found: $ZIP" >&2; exit 1; }

if [ "$DRY_RUN" = "1" ]; then
  echo "[dry-run] 将执行 / would run："
  echo "  git tag -a $TAG -m \"$TAG\""
  echo "  git push origin $BRANCH"
  echo "  git push origin $TAG"
  echo "  gh release create $TAG $ZIP --generate-notes"
  exit 0
fi

command -v gh >/dev/null 2>&1 || { echo "错误：未安装 GitHub CLI（gh）| Error: GitHub CLI (gh) not installed" >&2; exit 1; }
git remote get-url origin >/dev/null 2>&1 || { echo "错误：未配置 git remote origin | Error: no git remote 'origin' configured" >&2; exit 1; }

# 需要可用的 gh 鉴权（或提供 GH_TOKEN）；在打 tag 之前校验，避免留下没有 Release 的悬空 tag
# Require working gh auth (or GH_TOKEN); checked before tagging to avoid a dangling tag without a Release
if [ -z "${GH_TOKEN:-}" ]; then
  gh auth status >/dev/null 2>&1 || { echo "错误：gh 未登录，请先 gh auth login 或设置 GH_TOKEN | Error: gh is not authenticated; run gh auth login or set GH_TOKEN" >&2; exit 1; }
fi

git tag -a "$TAG" -m "PerformanceScope $TAG"
git push origin "$BRANCH"
git push origin "$TAG"
gh release create "$TAG" "$ZIP" --title "PerformanceScope $TAG" --generate-notes

echo "已发布 $TAG（产物：$ZIP）| Released $TAG (artifact: $ZIP)"
