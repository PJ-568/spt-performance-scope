# 原理与逆向细节

> 本文记录 PerformanceScope 涉及的《逃离塔科夫》客户端渲染管线细节，全部来自对本机安装的 SPT 4.1.x `Assembly-CSharp.dll` 的实际反编译。

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

> This document records the Escape from Tarkov client rendering-pipeline details behind PerformanceScope, all taken from decompiling the SPT 4.1.x `Assembly-CSharp.dll` installed on this machine.

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
