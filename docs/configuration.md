# 配置参考

配置文件：`BepInEx/config/com.pj568.performancescope.cfg`（也可用 BepInEx ConfigurationManager 修改）。

本插件不自带任何自定义界面或热键。安装 [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) 后，游戏内按 **F12**（SPT 默认；上游默认 F1，可在其配置文件中改键）打开菜单，在 `PerformanceScope` 分区调整；改动即时生效（事件驱动，无每帧轮询；为避免拖动滑块时反复重建 RenderTexture，变更在停止操作约 250 ms 后应用）。

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
| 3. 镜内贴图与细节 · Scope Textures & Details | 镜内贴图 mip 模式 \| Scope Mip Mode | `游戏默认` | 游戏默认 / 自定义。自定义时覆盖镜内相机的贴图 mip 偏差。 |
| 3. 镜内贴图与细节 · Scope Textures & Details | Mip 偏差 \| Mip Bias | `3` | `-2`…`8`；绝对值覆盖镜内 `streamingMipmapBias`，越大常驻 mip 越粗。 |
| 3. 镜内贴图与细节 · Scope Textures & Details | 关闭镜内泛光 \| Disable Scope Bloom | `false` | 勾选则关闭镜内相机的泛光（`bloomOptimized`）。 |
| 3. 镜内贴图与细节 · Scope Textures & Details | 关闭镜内终极泛光 \| Disable Scope Ultimate Bloom | `false` | 勾选则关闭镜内相机的终极泛光（`ultimateBloom`）。 |
| 3. 镜内贴图与细节 · Scope Textures & Details | 关闭镜内色散 \| Disable Scope Chromatic Aberration | `false` | 勾选则关闭镜内相机的色散（`chromaticAberration`）。 |
| 3. 镜内贴图与细节 · Scope Textures & Details | 关闭镜内鱼眼 \| Disable Scope Fisheye | `false` | 勾选则关闭镜内相机的鱼眼（`fisheye`）。 |

## 三种取值方式

- **游戏默认**：不改变游戏行为，插件按游戏硬编码的 `1024` 放行。安装后默认即此模式，需要显式选择其它模式才会生效。
- **屏幕高度比例**：镜内边长 = `round(屏幕高度 × 该值)`。比例的有效范围为 `0.05`–`4.0`，超出会被夹取到边界。
- **绝对像素**：直接指定镜内方形 RenderTexture 的边长，有效范围为 `64`–`4096`，超出会被夹取到边界。

任一模式下若配置值非法（例如无法解析），回退为游戏默认 `1024`。

## 瞄准中立即应用

默认开启。开启时，你在瞄准期间改动目标值会立即重建镜内 RenderTexture；关闭时，瞄准期间的改动会推迟到退出镜内后才应用。因为重建会销毁并重新创建 RenderTexture，瞄准中改值可能产生一帧闪烁，关闭此项可规避。

## 镜内贴图 mip

覆盖对象是镜内相机自己的 `UnityEngine.StreamingController.streamingMipmapBias`。游戏在 `EFT.CameraControl.OpticCameraManager.Init()` 中把它设为 `GraphicsSettingsGroup.TextureQualityToMipBias(TextureQuality)`，即 `Clamp(2 − 贴图品质, 0, 2)`（`TextureQuality` 取 0–2，因此游戏默认值落在 0–2 之间）；全仓库只有这一处写镜内相机的该值，且不会每帧刷新。

「镜内贴图 mip 模式」设为「自定义」时，插件按「Mip 偏差」的**绝对值**覆盖它。

这个值的语义需要说清楚：

- 它**不参与 GPU 采样时的 LOD 计算**，只决定流送系统让哪些 mip **常驻**。正值让常驻集合更粗，采样因此被夹到更粗的顶层——这才是「看起来更糊」的成因；它省的是**上传与磁盘 I/O（以及可能的常驻显存）**，不是 GPU 采样带宽。
- **只在开启贴图流送时生效**；关掉流送时游戏改用全局贴图上限，该偏差无任何作用。
- **受 `streamingMipmapsMaxLevelReduction` 截断**：游戏把它设为 `4 − 贴图品质`，贴图品质为「高」时只有 2 级可丢，因此偏差大于 `2` 的部分通常不会带来额外效果；该项还**优先于内存预算**。
- **常驻级别按所有启用相机中最精细的需求聚合**（游戏设了 `streamingMipmapsAddAllCameras = true`）。启用 DLSS/FSR 时主相机是 `−1.5`（更细），可能把纹理常驻「钉」在高分辨率上，此时镜内即使设正值也可能既省不到显存、也看不出变化。

覆盖只在镜内相机创建时执行一次即可持久，无需每帧。

> 注意：`streamingMipmapBias` 是绝对值覆盖，而非在游戏默认值上叠加偏移；想保持游戏行为就选「游戏默认」。

## 镜内细节

覆盖对象是镜内相机上由 `EFT.CameraControl.OpticComponentUpdater.CopyComponentFromOptic(OpticSight)` 按每瞄具 `ScopeEffectsData` 设置的组件开关；游戏**不会**每帧重写这些开关，因此插件在该方法的 postfix 里覆盖一次即可持久。

四个开关各自独立：勾选即关闭对应组件，取消勾选则还原为该瞄具的授权值；全部不勾选时完全保持游戏行为。

| 开关 | 关闭的组件 |
| --- | --- |
| 关闭镜内泛光 | `bloomOptimized` |
| 关闭镜内终极泛光 | `ultimateBloom` |
| 关闭镜内色散 | `chromaticAberration` |
| 关闭镜内鱼眼 | `fisheye` |

应用时机：每次换镜（`CopyComponentFromOptic` 因瞄具实例变化而执行）时在其 postfix 应用，配置变更时也重新应用一次。插件按瞄具实例缓存各开关的原始值，因此取消勾选能正确还原——游戏的 `cachedOpticId` 守卫会让同一瞄具重复进镜时跳过写入，若不按实例判断缓存，就会把我们关掉的值误当成原始值。刻意不暴露 `tonemapping`，因为关掉它会破坏镜内色彩与曝光。

### 为什么不提供体积光与散射

`OpticComponentUpdater.LateUpdate()` **每帧**把主相机的 `volumetricLightRenderer.enabled` / `.Resolution`、`undithering.enabled`、`tod_Scattering`、`mboit_Scattering` 覆盖到镜内相机；要修改它们必须在每帧强制覆盖。本插件选择不做，因此只提供第一类的四个开关。

## 注意

> `屏幕高度比例` 在 4K（2160p）下取 `0.7` 会得到 1512，**高于**游戏默认 1024，反而增加负载。降低镜内分辨率才有助于提升帧数。

===============================================================

# Configuration Reference

Config file: `BepInEx/config/com.pj568.performancescope.cfg` (or edit through BepInEx ConfigurationManager).

The plugin ships no custom UI or hotkeys. Install [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager), then press **F12** in game (the SPT default; upstream defaults to F1, rebindable in its config) and adjust the `PerformanceScope` sections; changes apply immediately (event-driven, no per-frame polling; to avoid repeated RenderTexture rebuilds while dragging a slider, a change is applied about 250 ms after you stop).

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
| 3. 镜内贴图与细节 · Scope Textures & Details | 镜内贴图 mip 模式 \| Scope Mip Mode | `游戏默认` | Game default / custom. Custom overrides the scope camera's texture mip bias. |
| 3. 镜内贴图与细节 · Scope Textures & Details | Mip 偏差 \| Mip Bias | `3` | `-2`…`8`; an absolute override of the scope `streamingMipmapBias`; larger keeps coarser mips resident. |
| 3. 镜内贴图与细节 · Scope Textures & Details | 关闭镜内泛光 \| Disable Scope Bloom | `false` | Check to disable the scope camera's bloom (`bloomOptimized`). |
| 3. 镜内贴图与细节 · Scope Textures & Details | 关闭镜内终极泛光 \| Disable Scope Ultimate Bloom | `false` | Check to disable the scope camera's ultimate bloom (`ultimateBloom`). |
| 3. 镜内贴图与细节 · Scope Textures & Details | 关闭镜内色散 \| Disable Scope Chromatic Aberration | `false` | Check to disable the scope camera's chromatic aberration (`chromaticAberration`). |
| 3. 镜内贴图与细节 · Scope Textures & Details | 关闭镜内鱼眼 \| Disable Scope Fisheye | `false` | Check to disable the scope camera's fisheye (`fisheye`). |

## The Three Resolution Modes

- **Game default**: changes nothing; the plugin passes through the game's hard-coded `1024`. This is the default on install, so the plugin has no effect until you pick another mode.
- **Screen-height ratio**: scope edge = `round(screen height × value)`. The valid ratio range is `0.05`–`4.0`; out-of-range values are clamped to the bounds.
- **Absolute pixels**: directly sets the edge length of the square scope RenderTexture. The valid range is `64`–`4096`; out-of-range values are clamped to the bounds.

In any mode, an invalid value (for example one that cannot be parsed) falls back to the game default `1024`.

## Apply While Scoped

On by default. When on, changing the target value while scoped rebuilds the scope RenderTexture immediately; when off, changes made while scoped are deferred until you unscope. Because a rebuild destroys and recreates the RenderTexture, changing the value while scoped may cause a one-frame flicker — turn this off to avoid it.

## Scope Mip

What is overridden is the scope camera's own `UnityEngine.StreamingController.streamingMipmapBias`. In `EFT.CameraControl.OpticCameraManager.Init()` the game sets it to `GraphicsSettingsGroup.TextureQualityToMipBias(TextureQuality)`, i.e. `Clamp(2 − texture quality, 0, 2)` (`TextureQuality` is 0–2, so the game default lands in the 0–2 range); this is the only place in the whole codebase that writes that value on the scope camera, and it is not refreshed every frame.

When "镜内贴图 mip 模式 | Scope Mip Mode" is set to "自定义" (custom), the plugin overrides it with the **absolute value** of "Mip 偏差 | Mip Bias".

The semantics of that value need to be stated clearly:

- It does **not** take part in the GPU sampling LOD calculation; it only decides which mips the streaming system keeps **resident**. A positive value makes the resident set coarser, so sampling is clamped to a coarser top level — that is what "looks blurrier" actually means. What it saves is **upload and disk I/O (and possibly resident VRAM)**, not GPU sampling bandwidth.
- It **only applies while texture streaming is enabled**; with streaming off the game uses a global texture limit instead and this bias does nothing.
- It is **truncated by `streamingMipmapsMaxLevelReduction`**: the game sets it to `4 − texture quality`, so at high texture quality only 2 levels can be dropped, and anything above `2` usually adds nothing; that setting also **takes priority over the memory budget**.
- **Residency aggregates the finest demand across all enabled cameras** (the game sets `streamingMipmapsAddAllCameras = true`). With DLSS/FSR enabled the main camera runs at `−1.5` (finer), which may pin the texture residency to a high resolution, in which case a positive in-scope value may save neither memory nor visible quality.

The override is applied once when the scope camera is created and persists, so no per-frame work is needed.

> Note: `streamingMipmapBias` is an absolute override, not an offset added on top of the game default; pick "游戏默认" (game default) to keep the game behavior.

## Scope Detail

What is overridden are the component toggles that `EFT.CameraControl.OpticComponentUpdater.CopyComponentFromOptic(OpticSight)` sets on the scope camera according to each optic's `ScopeEffectsData`; the game does **not** rewrite these toggles every frame, so the plugin overrides them once in that method's postfix and the result persists.

The four toggles are independent: checking one disables the matching component, unchecking it restores that optic's authored value, and leaving all of them unchecked keeps the game behavior entirely.

| Toggle | Component disabled |
| --- | --- |
| 关闭镜内泛光 (disable bloom) | `bloomOptimized` |
| 关闭镜内终极泛光 (disable ultimate bloom) | `ultimateBloom` |
| 关闭镜内色散 (disable chromatic aberration) | `chromaticAberration` |
| 关闭镜内鱼眼 (disable fisheye) | `fisheye` |

When it applies: once per optic change (when `CopyComponentFromOptic` runs because the optic instance changed) in its postfix, and again on a config change. The plugin caches the original values per optic instance so that unchecking a toggle restores correctly — the game's `cachedOpticId` guard skips the writes when you re-enter with the same optic, and without an instance-keyed cache our disabled values would be mistaken for the originals. `tonemapping` is deliberately not exposed, because disabling it would break the in-scope color and exposure.

### Why Volumetric Light and Scattering Are Not Offered

`OpticComponentUpdater.LateUpdate()` copies the main camera's `volumetricLightRenderer.enabled` / `.Resolution`, `undithering.enabled`, `tod_Scattering` and `mboit_Scattering` onto the scope camera **every frame**; changing them would require forcing an override every frame. This plugin chooses not to do so, and therefore offers only the four toggles of the first kind.

## Note

> With `屏幕高度比例`, `0.7` on a 4K (2160p) display yields 1512, which is **higher** than the game default 1024 and therefore costs more. Only lowering the scope resolution helps frame rate.
