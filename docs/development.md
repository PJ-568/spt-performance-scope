# 构建、测试、打包与发布

## 前置

本机需已安装 SPT 客户端。客户端程序集为专有文件，不随仓库分发。

## `SPT_DIR`

构建时通过环境变量 `SPT_DIR` 指定游戏根目录；未设置时回退到本机 Lutris 前缀的默认路径。

```shellscript
# 指定游戏根目录（未设置时回退到本机 Lutris 前缀默认路径）
export SPT_DIR="/path/to/Escape from Tarkov"
dotnet build src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj -c Release
```

Release 构建会自动把 dll 复制到 `$SPT_DIR/BepInEx/plugins/PerformanceScope/`；可用 `-p:DeployToGame=false` 关闭自动部署。

## 测试

纯逻辑层（分辨率换算与应用决策）无游戏依赖，可直接测试：

```shellscript
dotnet test tests/PerformanceScope.Tests/PerformanceScope.Tests.csproj
```

## 打包与发布

```shellscript
# 仅打包
scripts/package.sh
# 产物：artifacts/PerformanceScope-v{版本}.zip

# 一键发布：构建 → 打包 → 打 tag → 推送 → 创建 GitHub Release
scripts/release.sh
# 预演（只构建打包并打印将执行的命令，需工作区干净）
scripts/release.sh --dry-run
```

发布前置：已安装并完成鉴权的 GitHub CLI（`gh auth login`，或提供 `GH_TOKEN` 环境变量），且仓库已配置 `remote origin`。`release.sh` 会在打 tag 之前校验工作区干净、tag 不存在以及 gh 鉴权，发布后自动创建带 zip 附件的 Release。

## 部署（本机）

```shellscript
scripts/package.sh
# 解压 zip 到游戏根目录，或直接依赖 Release 构建的自动复制
```

## 为什么公共 CI 只跑纯逻辑单测

客户端插件必须引用专有程序集（`Assembly-CSharp.dll`），公共 runner 无法构建，故仓库只保留 `test.yml`（跑纯逻辑单测）；发行包用 `scripts/release.sh` 本地一键发布。

===============================================================

# Build, Test, Packaging and Release

## Prerequisites

A local SPT client install is required. The client assemblies are proprietary and are not distributed with this repository.

## `SPT_DIR`

Point at the game root with the `SPT_DIR` environment variable; when unset it falls back to the local Lutris prefix default path.

```shellscript
# Point at the game root (falls back to the local Lutris prefix by default)
export SPT_DIR="/path/to/Escape from Tarkov"
dotnet build src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj -c Release
```

A Release build copies the dll to `$SPT_DIR/BepInEx/plugins/PerformanceScope/` automatically; disable the automatic deploy with `-p:DeployToGame=false`.

## Test

The pure-logic layer (resolution math and apply decisions) has no game dependency and can be tested directly:

```shellscript
dotnet test tests/PerformanceScope.Tests/PerformanceScope.Tests.csproj
```

## Packaging and Release

```shellscript
# Package only
scripts/package.sh
# Output: artifacts/PerformanceScope-v{version}.zip

# One-command release: build → package → tag → push → create GitHub Release
scripts/release.sh
# Dry run (builds/packages and prints the commands; requires a clean work tree)
scripts/release.sh --dry-run
```

Release prerequisites: an installed and authenticated GitHub CLI (`gh auth login`, or a `GH_TOKEN` environment variable), and a configured `remote origin`. `release.sh` checks a clean work tree, a fresh tag and gh authentication before tagging, then creates a Release with the zip attached.

## Deploy (Local)

```shellscript
scripts/package.sh
# Extract the zip into the game root, or rely on the automatic copy of Release builds
```

## Why Public CI Runs Only Pure-Logic Unit Tests

A client plugin must reference proprietary assemblies (`Assembly-CSharp.dll`), which public runners cannot build, so the repository keeps only `test.yml` (pure-logic unit tests); release packages are produced locally with `scripts/release.sh`.
