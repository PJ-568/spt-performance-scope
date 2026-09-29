# 原理与逆向细节

> 本文记录 PerformanceScope 涉及的《Single Player Tushonka》客户端渲染管线细节，全部来自对本机安装的 SPT 4.1.x `Assembly-CSharp.dll` 的实际反编译。

## 镜内 PiP 管线

镜内画中画（PiP）为瞄具单独创建一台相机：`EFT.CameraControl.OpticCameraManager` 持有自己的 `Camera`，并为它渲染一个 `N×N` 的方形 RenderTexture，再贴到镜片上。

`SetResolution(int)` 是重建入口：每次调用先销毁旧 `_renderTexture`，随后按 `Camera.allowHDR` 在 `RenderTextureFormat.ARGBHalf` 与 `ARGB32` 之间二选一，新建边长 `N`、深度 24 的方形 RT，设置 `autoGenerateMips=false`、`useMipMap=false`、`name = "SSAAOpticCurrent"`，最后赋给 `Camera.targetTexture` 并 `Shader.SetGlobalTexture(_camTexId)`（`_CamTex`）。

`OpticCameraManager` 通过 `OnOpticEnabled` / `OnOpticDisabled` 事件向外部广播瞄具的启用与停用。

## 符号表

### `EFT.CameraControl.OpticCameraManager`

| 成员 | 形式 | 位置 |
| --- | --- | --- |
| `OpticFinalResolution` | `public int OpticFinalResolution = 1024;` | 行 17 |
| `OpticRenderResolution` | `public int OpticRenderResolution => OpticFinalResolution;` | 行 55 |
| `OpticNextRenderResolution` | `public int OpticNextRenderResolution => OpticFinalResolution;` | 行 57 |
| `Camera` | `public Camera Camera { get; set; }` | — |
| `IsAnyOpticCameraRendering` | `public bool IsAnyOpticCameraRendering => CurrentOpticSight != null;` | — |
| `Init()` | 游戏侧唯一调用 `SetResolution(OpticFinalResolution)`；同时 `OpticSight.OpticSightState.Bind(...)` | 行 195、197 |
| `SetResolution(int)` | 销毁旧 `_renderTexture`，按 `Camera.allowHDR` 选格式，新建方形 RT 并写入 `Camera.targetTexture` 与 `_CamTex` | 行 205–221 |
| 事件 | `OnOpticEnabled` / `OnOpticDisabled` | — |

### 全局贴图

| 成员 | 形式 | 位置 |
| --- | --- | --- |
| `_CamTex` | `Shader.SetGlobalTexture(_camTexId)`，`_camTexId` 定义为 `_CamTex` | 行 53 |

### `EFT.CameraControl.CameraManager`

| 成员 | 形式 | 位置 |
| --- | --- | --- |
| `instance` | `public static CameraManager instance` | 行 62 |
| `Instance` | `public static CameraManager Instance => instance ?? (instance = new CameraManager());` | 行 150 |
| `OpticCameraManager` | 属性，返回 `OpticCameraManager` | 行 155 |
| 构造函数 | `new OpticCameraManager()` | 行 1259–1261 |
| 战局相机初始化 | 调用 `OpticCameraManager.Init()` | 行 550 |

### `EFT.CameraControl.OpticSight`

| 成员 | 形式 | 位置 |
| --- | --- | --- |
| `OpticSightState` | `public static BindableState<OpticSightStatus> OpticSightState` | 行 18 |
| `OpticSightStatus` | `{ OpticSight OpticSight; bool IsEnabled; }` | — |
| `OnEnable` / `OnDisable` | 写入 `OpticSightState.Value` | — |

### 超分组件

| 成员 | 形式 | 位置 |
| --- | --- | --- |
| `SSAA` / `SSAAImpl` | 仅挂在主相机上 | `CameraManager.cs:529-530` |
| 超分入口 | `SetAntiAliasing` / `SetSuperSampling` / `SetFSR2` / `SetFSR3` | — |

## 为什么用 Harmony prefix 覆盖入参

游戏侧唯一调用 `SetResolution` 的地方是 `Init()`（行 195），且固定传入 `OpticFinalResolution`。如果在 postfix 里改写结果，游戏会先按默认 `1024` 建一次 RenderTexture，再被插件重建，造成一次可见的闪烁。

因此本插件用 Harmony **prefix** 直接覆盖 `SetResolution` 的入参，让游戏自身的 `Init()` 一次性就建到目标分辨率，避免二次重建。

## 应用时机（事件驱动）

插件不做任何每帧轮询，只在三类时机工作：

1. **战局开始**：游戏的 `Init()` 调用 `SetResolution`，prefix 直接把入参改成目标值，RT 一次建成，无需运行时补偿。
2. **配置变更**：插件订阅 `ConfigFile.SettingChanged`，防抖 250 ms 后应用一次（ConfigurationManager 拖动滑块时几乎每帧写值，逐次应用会连续重建）；未瞄准则应用，正在瞄准且“瞄准中立即应用”为真时也应用。
3. **退出镜内**：订阅 `OpticCameraManager.OnOpticDisabled`（在 `CurrentOpticSight` 置空且镜内相机停用之后触发，是安全的重建窗口），在此补齐被推迟的改动。

订阅按 `OpticCameraManager` 实例幂等：战局切换导致管理器重建时，prefix 会把新实例交给服务并退订旧实例。

## 镜内贴图 mip 与细节的可覆盖点

覆盖镜内贴图 mip 与镜内细节的写入点共两处，本插件只介入 `CopyComponentFromOptic`，刻意不碰 `LateUpdate`。

### 镜内贴图 mip

`EFT.CameraControl.OpticCameraManager.Init()` 会把镜内相机自己的 `UnityEngine.StreamingController.streamingMipmapBias` 设为 `GraphicsSettingsGroup.TextureQualityToMipBias(TextureQuality)`，映射为 `Clamp(2 − 画质, 0, 2)`（`TextureQuality` 取 0–2）。全仓库只有这一处写镜内相机的该值。因此插件在镜内相机创建时覆盖一次即可持久，**不需要每帧**。

### 镜内细节

`EFT.CameraControl.OpticComponentUpdater.CopyComponentFromOptic(OpticSight)` 会按每瞄具的 `ScopeEffectsData` 设置镜内相机上的组件开关（`chromaticAberration`、`bloomOptimized`、`ultimateBloom`、`fisheye`、`cc_FastVignette`、`tonemapping` 等）。**这些不会被每帧覆盖**，所以插件用该方法的 postfix 覆盖一次即可持久。插件对其中四个提供**逐项开关**（泛光、终极泛光、色散、鱼眼），并按瞄具实例缓存原始值，使取消勾选能正确还原。

### 刻意不做体积光与散射

`OpticComponentUpdater.LateUpdate()` **每帧**把主相机的 `volumetricLightRenderer.enabled` / `.Resolution`、`undithering.enabled`、`tod_Scattering`、`mboit_Scattering` 覆盖到镜内相机，要改它们必须每帧强制覆盖；本插件选择不做，只为第一类提供四个开关（外加不暴露 `tonemapping`，因为关掉它会破坏镜内色彩与曝光）。

新增的 Harmony 目标是 `OpticComponentUpdater.CopyComponentFromOptic`（postfix）；**不 patch** `LateUpdate` 或 `Awake`。

### 两条写入路径

| 路径 | 时机 | 覆盖的组件 |
| --- | --- | --- |
| `CopyComponentFromOptic` | 换镜/进镜时一次（瞄具实例变化时执行） | 色散、泛光、终极泛光、鱼眼、暗角、色调映射、NV/热成像等 |
| `LateUpdate` | 每帧（仅镜内相机激活时） | undithering、体积光（含 Resolution）、TOD/MBOIT 散射、postProcessLayer/TAA |

## 尺寸消费方自动重建

镜内尺寸变化不需要插件干预。以下子系统会在后续帧自检并重建：

| 消费方 | 关注点 |
| --- | --- |
| `VolumetricLightRenderer` | 读 `OpticRenderResolution` |
| `GPUInstancer` / `GPUInstancerManager` | `OpticResolution`、`OnResolutionChangeOptic` |
| `DistantShadow` | 自检尺寸 |
| `WindowsManager`（MBOIT） | `CameraRenderData` 尺寸比对 |

## 超分限制

镜内画面最终由主相机的 `SSAA` / `SSAAImpl` 随整帧一起上采样到屏幕。全游戏只有一套挂在主相机上的超分组件（`CameraManager.cs:529-530`），镜内**没有**独立的 SSAA / 超分实例。因此本插件**无法独立设置镜内超分档**，也不切换任何超分设置，只调整镜内源分辨率。

## 1024 的来历

游戏默认的 `1024` 是硬编码常量（`OpticFinalResolution = 1024`），与屏幕分辨率、画质档、瞄具倍率、镜型都无关。`OpticRenderResolution` 与 `OpticNextRenderResolution` 都只是回读 `OpticFinalResolution`；其中 `OpticNextRenderResolution` 是未接线的残留，没有实际消费方。

===============================================================

# Principles and Reverse-Engineering Details

> This document records the Single Player Tushonka client rendering-pipeline details behind PerformanceScope, all taken from decompiling the SPT 4.1.x `Assembly-CSharp.dll` installed on this machine.

## The In-Scope PiP Pipeline

Picture-in-picture (PiP) creates a dedicated camera for the optic: `EFT.CameraControl.OpticCameraManager` owns its own `Camera` and renders an `N×N` square RenderTexture for it, then maps that onto the lens.

`SetResolution(int)` is the rebuild entry point: each call first destroys the old `_renderTexture`, then picks `RenderTextureFormat.ARGBHalf` or `ARGB32` based on `Camera.allowHDR`, creates a square RT of edge length `N` and depth 24, sets `autoGenerateMips=false`, `useMipMap=false`, `name = "SSAAOpticCurrent"`, and finally assigns it to `Camera.targetTexture` and calls `Shader.SetGlobalTexture(_camTexId)` (`_CamTex`).

`OpticCameraManager` broadcasts optic enable/disable through the `OnOpticEnabled` / `OnOpticDisabled` events.

## Symbol Table

### `EFT.CameraControl.OpticCameraManager`

| Member | Form | Location |
| --- | --- | --- |
| `OpticFinalResolution` | `public int OpticFinalResolution = 1024;` | line 17 |
| `OpticRenderResolution` | `public int OpticRenderResolution => OpticFinalResolution;` | line 55 |
| `OpticNextRenderResolution` | `public int OpticNextRenderResolution => OpticFinalResolution;` | line 57 |
| `Camera` | `public Camera Camera { get; set; }` | — |
| `IsAnyOpticCameraRendering` | `public bool IsAnyOpticCameraRendering => CurrentOpticSight != null;` | — |
| `Init()` | the only game-side call site of `SetResolution(OpticFinalResolution)`; also `OpticSight.OpticSightState.Bind(...)` | lines 195, 197 |
| `SetResolution(int)` | destroys the old `_renderTexture`, picks the format from `Camera.allowHDR`, creates the square RT and writes `Camera.targetTexture` plus `_CamTex` | lines 205–221 |
| Events | `OnOpticEnabled` / `OnOpticDisabled` | — |

### Global texture

| Member | Form | Location |
| --- | --- | --- |
| `_CamTex` | `Shader.SetGlobalTexture(_camTexId)`, where `_camTexId` is `_CamTex` | line 53 |

### `EFT.CameraControl.CameraManager`

| Member | Form | Location |
| --- | --- | --- |
| `instance` | `public static CameraManager instance` | line 62 |
| `Instance` | `public static CameraManager Instance => instance ?? (instance = new CameraManager());` | line 150 |
| `OpticCameraManager` | property returning `OpticCameraManager` | line 155 |
| Constructor | `new OpticCameraManager()` | lines 1259–1261 |
| Raid camera initialization | calls `OpticCameraManager.Init()` | line 550 |

### `EFT.CameraControl.OpticSight`

| Member | Form | Location |
| --- | --- | --- |
| `OpticSightState` | `public static BindableState<OpticSightStatus> OpticSightState` | line 18 |
| `OpticSightStatus` | `{ OpticSight OpticSight; bool IsEnabled; }` | — |
| `OnEnable` / `OnDisable` | write to `OpticSightState.Value` | — |

### Upscaler components

| Member | Form | Location |
| --- | --- | --- |
| `SSAA` / `SSAAImpl` | attached to the main camera only | `CameraManager.cs:529-530` |
| Upscaler entries | `SetAntiAliasing` / `SetSuperSampling` / `SetFSR2` / `SetFSR3` | — |

## Why a Harmony Prefix Overrides the Argument

The only game-side call site of `SetResolution` is `Init()` (line 195), and it always passes `OpticFinalResolution`. If the result were rewritten in a postfix, the game would first build a RenderTexture at the default `1024` and then have it rebuilt by the plugin, causing a visible flicker.

The plugin therefore uses a Harmony **prefix** to override the argument of `SetResolution`, so the game's own `Init()` creates the RT at the target resolution directly, avoiding a double rebuild.

## When It Applies (Event-Driven)

The plugin does no per-frame polling; it only acts on three triggers:

1. **Raid start**: the game's `Init()` calls `SetResolution`, and the prefix replaces the argument with the target value, so the RT is created once at the right size with no runtime compensation.
2. **Config change**: the plugin subscribes to `ConfigFile.SettingChanged` and applies once after a 250 ms debounce (dragging a ConfigurationManager slider writes the value on almost every frame, and applying each time would rebuild repeatedly); it applies when not scoped, and also when scoped if "Apply While Scoped" is on.
3. **Scope exit**: it subscribes to `OpticCameraManager.OnOpticDisabled` (fired after `CurrentOpticSight` is cleared and the optic camera is deactivated — a safe rebuild window), where deferred changes are applied.

Subscriptions are idempotent per `OpticCameraManager` instance: when a raid change rebuilds the manager, the prefix hands the new instance to the service and unsubscribes the old one.

## Overridable Points for In-Scope Mip and Detail

There are two write points for the in-scope texture mip and the in-scope details; this plugin only touches `CopyComponentFromOptic` and deliberately leaves `LateUpdate` alone.

### Scope texture mip

`EFT.CameraControl.OpticCameraManager.Init()` sets the scope camera's own `UnityEngine.StreamingController.streamingMipmapBias` to `GraphicsSettingsGroup.TextureQualityToMipBias(TextureQuality)`, which maps to `Clamp(2 − texture quality, 0, 2)` (`TextureQuality` is 0–2). This is the only place in the whole codebase that writes that value on the scope camera. The plugin therefore overrides it once when the scope camera is created, and it persists — **no per-frame work is needed**.

### In-scope details

`EFT.CameraControl.OpticComponentUpdater.CopyComponentFromOptic(OpticSight)` sets the component toggles on the scope camera according to each optic's `ScopeEffectsData` (`chromaticAberration`, `bloomOptimized`, `ultimateBloom`, `fisheye`, `cc_FastVignette`, `tonemapping`, etc.). **These are not overwritten every frame**, so the plugin overrides them once in that method's postfix and the result persists. The plugin exposes **individual toggles** for four of them (bloom, ultimate bloom, chromatic aberration, fisheye) and caches the original values per optic instance so that unchecking a toggle restores correctly.

### Volumetric light and scattering deliberately excluded

`OpticComponentUpdater.LateUpdate()` copies the main camera's `volumetricLightRenderer.enabled` / `.Resolution`, `undithering.enabled`, `tod_Scattering` and `mboit_Scattering` onto the scope camera **every frame**; changing them would require forcing an override every frame. This plugin chooses not to do so and offers individual toggles only for the first kind (and it does not expose `tonemapping`, because disabling it would break the in-scope color and exposure).

The new Harmony target is `OpticComponentUpdater.CopyComponentFromOptic` (postfix); it does **not** patch `LateUpdate` or `Awake`.

### The Two Write Paths

| Path | Timing | Components overridden |
| --- | --- | --- |
| `CopyComponentFromOptic` | once per optic change / scope entry (runs when the optic instance changes) | chromatic aberration, bloom, ultimate bloom, fisheye, vignette, tonemapping, NV/thermal, etc. |
| `LateUpdate` | every frame (only while the scope camera is active) | undithering, volumetric light (including Resolution), TOD/MBOIT scattering, postProcessLayer/TAA |

## Size Consumers Rebuild Automatically

An in-scope size change needs no plugin intervention. The following subsystems detect it on a later frame and rebuild:

| Consumer | Concern |
| --- | --- |
| `VolumetricLightRenderer` | reads `OpticRenderResolution` |
| `GPUInstancer` / `GPUInstancerManager` | `OpticResolution`, `OnResolutionChangeOptic` |
| `DistantShadow` | self-checks the size |
| `WindowsManager` (MBOIT) | compares `CameraRenderData` size |

## Upscaler Limitation

The scoped image is finally upscaled to the screen together with the whole frame by the main camera's `SSAA` / `SSAAImpl`. The game has a single upscaler component attached to the main camera (`CameraManager.cs:529-530`); there is **no** independent in-scope SSAA / upscaler instance. The plugin therefore **cannot set an independent in-scope upscaler mode**, and it switches no upscaler setting; it only adjusts the in-scope source resolution.

## Where 1024 Comes From

The game default `1024` is a hard-coded constant (`OpticFinalResolution = 1024`), independent of screen resolution, quality preset, optic magnification, and optic type. Both `OpticRenderResolution` and `OpticNextRenderResolution` merely read back `OpticFinalResolution`; `OpticNextRenderResolution` is unwired residue with no actual consumer.
