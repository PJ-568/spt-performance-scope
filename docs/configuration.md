# 配置参考

配置文件：`BepInEx/config/com.pj568.performancescope.cfg`（也可用 BepInEx ConfigurationManager 修改）。

本插件不自带任何自定义界面或热键。安装 [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) 后，游戏内按 **F12**（SPT 默认；上游默认 F1，可在其配置文件中改键）打开菜单，在 `PerformanceScope` 分区调整；改动由服务层每帧同步，在游戏内即时生效。

## 配置表

| 节 | 键 | 默认值 | 说明 |
| --- | --- | --- | --- |
| 1. 通用 · General | 启用模组 \| Enable Mod | `true` | 总开关，关闭后恢复游戏默认分辨率。 |
| 1. 通用 · General | 启用日志 \| Enable Logging | `false` | 输出常规/诊断/警告日志（错误日志始终输出）。 |
| 1. 通用 · General | 详细日志 \| Verbose Logging | `false` | 更详细的诊断信息。 |
| 2. 镜内分辨率 · Scope Resolution | 取值方式 \| Resolution Mode | `游戏默认` | 游戏默认 / 屏幕高度比例 / 绝对像素。 |
| 2. 镜内分辨率 · Scope Resolution | 屏幕高度比例 \| Screen Height Ratio | `0.5` | 镜内边长 = round(屏幕高度 × 该值)。 |
| 2. 镜内分辨率 · Scope Resolution | 绝对像素 \| Absolute Pixels | `1024` | 镜内方形 RenderTexture 的边长。 |
| 2. 镜内分辨率 · Scope Resolution | 瞄准中立即应用 \| Apply While Scoped | `true` | 关闭时，瞄准期间的改动推迟到退出镜内后应用。 |

## 三种取值方式

- **游戏默认**：不改变游戏行为，插件按游戏硬编码的 `1024` 放行。安装后默认即此模式，需要显式选择其它模式才会生效。
- **屏幕高度比例**：镜内边长 = `round(屏幕高度 × 该值)`。比例的有效范围为 `0.05`–`4.0`，超出会被夹取到边界。
- **绝对像素**：直接指定镜内方形 RenderTexture 的边长，有效范围为 `64`–`4096`，超出会被夹取到边界。

任一模式下若配置值非法（例如无法解析），回退为游戏默认 `1024`。

## 瞄准中立即应用

默认开启。开启时，你在瞄准期间改动目标值会立即重建镜内 RenderTexture；关闭时，瞄准期间的改动会推迟到退出镜内后才应用。因为重建会销毁并重新创建 RenderTexture，瞄准中改值可能产生一帧闪烁，关闭此项可规避。

## 注意

> `屏幕高度比例` 在 4K（2160p）下取 `0.7` 会得到 1512，**高于**游戏默认 1024，反而增加负载。降低镜内分辨率才有助于提升帧数。

===============================================================

# Configuration Reference

Config file: `BepInEx/config/com.pj568.performancescope.cfg` (or edit through BepInEx ConfigurationManager).

The plugin ships no custom UI or hotkeys. Install [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager), then press **F12** in game (the SPT default; upstream defaults to F1, rebindable in its config) and adjust the `PerformanceScope` sections; changes are synced every frame by the service layer and apply immediately in game.

## Config Table

| Section | Key | Default | Notes |
| --- | --- | --- | --- |
| 1. 通用 · General | 启用模组 \| Enable Mod | `true` | Master switch; when off the game default is restored. |
| 1. 通用 · General | 启用日志 \| Enable Logging | `false` | Writes info/verbose/warning logs (errors are always logged). |
| 1. 通用 · General | 详细日志 \| Verbose Logging | `false` | More detailed diagnostics. |
| 2. 镜内分辨率 · Scope Resolution | 取值方式 \| Resolution Mode | `游戏默认` | Game default / screen-height ratio / absolute pixels. |
| 2. 镜内分辨率 · Scope Resolution | 屏幕高度比例 \| Screen Height Ratio | `0.5` | Scope edge = round(screen height × value). |
| 2. 镜内分辨率 · Scope Resolution | 绝对像素 \| Absolute Pixels | `1024` | Edge length of the square scope RenderTexture. |
| 2. 镜内分辨率 · Scope Resolution | 瞄准中立即应用 \| Apply While Scoped | `true` | When off, changes made while scoped are deferred until you unscope. |

## The Three Resolution Modes

- **Game default**: changes nothing; the plugin passes through the game's hard-coded `1024`. This is the default on install, so the plugin has no effect until you pick another mode.
- **Screen-height ratio**: scope edge = `round(screen height × value)`. The valid ratio range is `0.05`–`4.0`; out-of-range values are clamped to the bounds.
- **Absolute pixels**: directly sets the edge length of the square scope RenderTexture. The valid range is `64`–`4096`; out-of-range values are clamped to the bounds.

In any mode, an invalid value (for example one that cannot be parsed) falls back to the game default `1024`.

## Apply While Scoped

On by default. When on, changing the target value while scoped rebuilds the scope RenderTexture immediately; when off, changes made while scoped are deferred until you unscope. Because a rebuild destroys and recreates the RenderTexture, changing the value while scoped may cause a one-frame flicker — turn this off to avoid it.

## Note

> With `屏幕高度比例`, `0.7` on a 4K (2160p) display yields 1512, which is **higher** than the game default 1024 and therefore costs more. Only lowering the scope resolution helps frame rate.
