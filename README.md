# PerformanceScope

> 调整《逃离塔科夫》光学瞄具**镜内放大（PiP）相机**渲染分辨率的 SPT 客户端插件。

镜内画中画（PiP）是原生瞄具最吃 GPU 的部分：游戏会为瞄具单独创建一台相机，把一个 `N×N` 的方形 RenderTexture（默认 `1024²`）贴到镜片上。本插件让这个 `N` 可配置，从而在画质与帧数之间自行取舍。

## 原理

客户端反编译（EFT 0.16 / SPT 4.1.x）确认：

- `EFT.CameraControl.OpticCameraManager` 中字段 `public int OpticFinalResolution = 1024;`，方法 `SetResolution(int)` 会销毁并重建一个 `N×N` 方形 `RenderTexture`（`name = "SSAAOpticCurrent"`），赋给 `Camera.targetTexture` 并 `Shader.SetGlobalTexture("_CamTex")`。
- 该值在当前版本**没有任何设置项暴露**，唯一调用点是 `Init()`。
- 体积光（`VolumetricLightRenderer`）、草地运动矢量（`GPUInstancer`）、远距阴影（`DistantShadow`）、MBOIT（`WindowsManager`）都会在下一帧检测到尺寸变化并自动重建，无需插件干预。

因此本插件用 Harmony **prefix** 覆盖 `SetResolution` 的入参，使游戏自身的 `Init()` 就直接建到目标分辨率，避免二次重建造成的闪烁。

镜内画面最终由主相机的 `SSAA`/`SSAAImpl` 随整帧上采样到屏幕——**本插件不改变超分（DLSS/FSR）设置**，只调整镜内源分辨率。

## 支持版本

- SPT `4.1.x`（EFT 客户端 `0.16`）。
- 客户端插件依赖 `Assembly-CSharp.dll` 中的具体类型与方法，跨版本可能失效。

## 安装

1. 构建或下载 `PerformanceScope-v{版本}.zip`。
2. 解压到游戏根目录，使 dll 位于
   `BepInEx/plugins/PerformanceScope/PerformanceScope.dll`。
3. 启动游戏。

## 调参方式

安装 [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) 后，
游戏内按 **F1**（可在其配置文件中改键）打开菜单，在 `PerformanceScope` 分区调整。
**本插件不自带任何自定义界面或热键**；改动在游戏内即时生效（由服务层每帧同步）。

## 配置

配置文件：`BepInEx/config/com.pj568.performancescope.cfg`（也可用 ConfigurationManager 修改）。

| 节 | 键 | 默认值 | 说明 |
| --- | --- | --- | --- |
| 1. 通用 | 启用模组 | `true` | 总开关，关闭后恢复游戏默认分辨率。 |
| 1. 通用 | 启用日志 | `false` | 输出常规/诊断/警告日志（错误日志始终输出）。 |
| 1. 通用 | 详细日志 | `false` | 更详细的诊断信息。 |
| 2. 镜内分辨率 | 取值方式 | `游戏默认` | 游戏默认 / 屏幕高度比例 / 绝对像素。 |
| 2. 镜内分辨率 | 屏幕高度比例 | `0.5` | 镜内边长 = round(屏幕高度 × 该值)。 |
| 2. 镜内分辨率 | 绝对像素 | `1024` | 镜内方形 RenderTexture 的边长。 |
| 2. 镜内分辨率 | 瞄准中立即应用 | `true` | 关闭时，瞄准期间的改动推迟到退出镜内后应用。 |

> 默认取值方式为「游戏默认」，即安装后不改变游戏行为；请显式选择模式。
> 注意：`屏幕高度比例` 在 4K（2160p）下 `0.7` 会得到 1512，**高于**游戏默认 1024，反而增加负载。

## 构建

前置：本机已安装 SPT 客户端（客户端程序集为专有文件，不随仓库分发）。

```shellscript
# 指定游戏根目录（未设置时回退到本机 Lutris 前缀默认路径）
export SPT_DIR="/path/to/Escape from Tarkov"
dotnet build src/PerformanceScope.Plugin/PerformanceScope.Plugin.csproj -c Release
```

Release 构建会自动把 dll 复制到 `$SPT_DIR/BepInEx/plugins/PerformanceScope/`；
可用 `-p:DeployToGame=false` 关闭。

## 测试

纯逻辑层（分辨率换算与应用决策）无游戏依赖，可直接测试：

```shellscript
dotnet test tests/PerformanceScope.Tests/PerformanceScope.Tests.csproj
```

## 打包

```shellscript
scripts/package.sh
# 产物：artifacts/PerformanceScope-v{版本}.zip
```

## 部署（本机）

```shellscript
scripts/package.sh
# 解压 zip 到游戏根目录，或直接依赖 Release 构建的自动复制
```

启动后 `BepInEx/LogOutput.log` 应出现：

```
[Info   :PerformanceScope] PerformanceScope 已加载（在 ConfigurationManager 菜单中调参）
[Info   :PerformanceScope] 镜内分辨率 1024 → 720
```

## 验证

1. 在 ConfigurationManager 里开启「启用日志」。
2. 进入战局，按住右键进入瞄具。
3. 打开 ConfigurationManager（默认 F1），把「取值方式」切到「屏幕高度比例」或「绝对像素」。
4. 观察 `BepInEx/LogOutput.log` 的 `镜内分辨率 1024 → …` 与镜内画质/帧数变化。
5. 关闭「瞄准中立即应用」后瞄准时改值，确认改动在退出镜内后才生效。

## 与其它模组的交互

- **PiP-Disabler**（`com.fiodor.pipdisabler`）：它是对 `SetResolution` 的 **postfix 门控**（不改写入参、不改字段），与本插件共存；被它抑制的镜内渲染不会触发本插件的冲突重试。
- **Fontaine's FOV Fix / Amands's Graphics**：patch 的是 `OpticComponentUpdater` 等与本插件不重叠的目标，无直接冲突。
- **DERP（Dynamic External Resolution Patch）**：改的是**主相机/全局**分辨率与超分档，与本插件（镜内方形 RT 尺寸）机制不同；可同时使用，但两者都会影响分辨率，注意叠加效果。
- **DLSS / FSR**：本插件**不切换超分档**；但开启超分时改变镜内 RT 尺寸可能产生一帧闪烁，可关闭「瞄准中立即应用」规避。SPT 官方亦记录过「DLSS/FSR 下进镜切换档位会黑屏闪烁」。

> 本插件涉及的游戏符号（`OpticCameraManager.SetResolution`、`OpticFinalResolution`、`_CamTex`、`SSAAOpticCurrent` 等）均直接取自**本机安装的 SPT 4.1.x 实际 `Assembly-CSharp.dll`** 反编译结果，而非公开资料。

## 已知限制与风险

- **无法独立设置镜内超分档**：全游戏只有一套挂在主相机上的 `SSAA`/`SSAAImpl`，镜内画面是其一部分；独立的镜内 FSR/DLSS 需要另挂一套组件，本插件不做。
- **瞄准中改分辨率可能有一帧闪烁**：因为会销毁并重建 RenderTexture；可将「瞄准中立即应用」关闭以规避。
- **镜内低分辨率下纹理锯齿会更明显**：镜内相机的 mipmap bias 只在 `Init()` 时设置一次，插件不做补偿。
- **与其它改 `SetResolution` 的模组可能冲突**：若第三方 prefix 改写同一入参，插件检测到目标未生效后会暂停重试直至你更改目标值，避免每帧重建 RenderTexture。
- **版本敏感**：依赖具体类型与方法，EFT 更新后可能失效。
- **CI 限制**：公共 runner 无法构建客户端插件（缺少专有程序集），`test.yml` 只跑纯逻辑单测；发行包需在装有 SPT 的机器（或自托管 runner）上通过 `scripts/package.sh` 生成。

## 许可证

（待定）
