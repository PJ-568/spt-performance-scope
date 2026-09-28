%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

# PerformanceScope

> 调整《逃离塔科夫》光学瞄具**镜内放大（PiP）相机**渲染分辨率的 SPT 客户端插件。

镜内画中画（PiP）是原生瞄具最吃 GPU 的部分：游戏会为瞄具单独创建一台相机，把一张 `N×N` 的方形 RenderTexture（默认 `1024²`）贴到镜片上。本插件让这个 `N` 可配置，从而在画质与帧数之间自行取舍。

## 安装

1. 下载 Releases 中的 `PerformanceScope-v{版本}.zip`。
2. 解压到游戏根目录，使 dll 位于
   `BepInEx/plugins/PerformanceScope/PerformanceScope.dll`。
3. 启动游戏。

## 快速上手

1. 确认已安装 [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)。
2. 游戏内按 **F1**（可在其配置中改键）打开菜单，找到 `PerformanceScope` 分区。
3. 把「取值方式 | Resolution Mode」设为「屏幕高度比例」或「绝对像素」——
   默认是「游戏默认」，安装后不改变游戏行为。
4. 进战局举镜查看效果；开启「启用日志」后可在
   `BepInEx/LogOutput.log` 看到 `镜内分辨率 1024 → …`。

> 注意：`屏幕高度比例` 在 4K（2160p）下取 `0.7` 会得到 1512，**高于**默认 1024，反而更吃性能。

## 文档

- [原理与逆向细节](docs/principle.md)
- [配置参考](docs/configuration.md)
- [构建、测试与发布](docs/development.md)
- [实机验证](docs/verification.md)
- [兼容性与限制](docs/compatibility.md)

## 支持版本

SPT `4.1.x`（EFT 客户端 `0.16`）。

## 许可证

[MIT](LICENSE)

===============================================================

# PerformanceScope

> An SPT client plugin that adjusts the render resolution of the **in-scope (PiP) camera** for magnified optics in Escape from Tarkov.

Picture-in-picture (PiP) is the most GPU-hungry part of a magnified optic: the game creates a dedicated camera and renders an `N×N` square RenderTexture (default `1024²`) onto the lens. This plugin makes that `N` configurable, so you can trade image quality for frame rate.

## Installation

1. Download `PerformanceScope-v{version}.zip` from Releases.
2. Extract it into the game root so the dll lands at
   `BepInEx/plugins/PerformanceScope/PerformanceScope.dll`.
3. Launch the game.

## Quick start

1. Make sure [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) is installed.
2. Press **F1** in game (rebindable in its config) and find the `PerformanceScope` sections.
3. Set "取值方式 | Resolution Mode" to "屏幕高度比例" (screen-height ratio) or "绝对像素" (absolute pixels) — the default is "游戏默认" (game default), which changes nothing.
4. Enter a raid and aim down sights to see the effect; enable "启用日志 | Enable Logging" to watch `镜内分辨率 1024 → …` in `BepInEx/LogOutput.log`.

> Note: with `屏幕高度比例`, a value of `0.7` on 4K (2160p) yields 1512, **higher** than the default 1024 and therefore more expensive.

## Documentation

- [How it works & reversing notes](docs/principle.md)
- [Configuration reference](docs/configuration.md)
- [Build, test & release](docs/development.md)
- [In-game verification](docs/verification.md)
- [Compatibility & limitations](docs/compatibility.md)

## Supported versions

SPT `4.1.x` (EFT client `0.16`).

## License

[MIT](LICENSE)

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
