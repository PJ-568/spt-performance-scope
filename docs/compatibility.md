# 兼容性与限制

## 与其它模组的交互

- **PiP-Disabler**（`com.fiodor.pipdisabler`）：它是对 `SetResolution` 的 **postfix 门控**（不改写入参、不改字段），与本插件共存；被它抑制的镜内渲染不会触发本插件的冲突重试。
- **Fontaine's FOV Fix / Amands's Graphics**：patch 的是 `OpticComponentUpdater` 等与本插件不重叠的目标，无直接冲突。
- **DERP（Dynamic External Resolution Patch）**：DERP 只对**主相机**操作——patch `OpticSight.OnEnable` / `OpticSight.OnDisable` / `Player.FirearmController.ChangeAimingMode`，并在 `EFT.CameraControl.CameraManager` 上调用 `SSAAImpl.Switch` / `SetAntiAliasing` / `SetFSR2` / `SetFSR3`。它既不 patch `OpticCameraManager.SetResolution`，也不改 `OpticFinalResolution`，与本插件**没有重叠的 patch 目标或共享状态**，可以同时使用；效果叠加：镜外由 DERP 降，镜内由本插件降。注意两点：① DERP 自身在 DLSS/FSR 下进镜会黑屏闪烁（官方建议用 TAA + Sampling Downgrade 规避）；② 两者同时降低分辨率时，镜片区域会被“双重降质”，通常比单独用任一都更糊。
- **DLSS / FSR**：本插件**不切换超分档**；但开启超分时改变镜内 RT 尺寸可能产生一帧闪烁，可关闭“瞄准中立即应用”规避。SPT 官方亦记录过“DLSS/FSR 下进镜切换档位会黑屏闪烁”。

## 已知限制与风险

- **无法独立设置镜内超分档**：全游戏只有一套挂在主相机上的 `SSAA`/`SSAAImpl`，镜内画面是其一部分；独立的镜内 FSR/DLSS 需要另挂一套组件，本插件不做。
- **瞄准中改分辨率可能有一帧闪烁**：因为会销毁并重建 RenderTexture；可将“瞄准中立即应用”关闭以规避。
- **镜内低分辨率下纹理锯齿会更明显**：镜内相机的 mipmap bias 只在 `Init()` 时设置一次，插件不做补偿。
- **与其它改 `SetResolution` 的模组可能冲突**：若第三方 prefix 改写同一入参，插件检测到目标未生效后会暂停重试直至你更改目标值，避免反复重建 RenderTexture。
- **版本敏感**：依赖具体类型与方法，EFT 更新后可能失效。
- **CI 限制**：客户端插件必须引用专有程序集，公共 runner 无法构建，故仓库只保留 `test.yml`（跑纯逻辑单测）；发行包用 `scripts/release.sh` 本地一键发布。

## 符号来源

> 本插件涉及的游戏符号（`OpticCameraManager.SetResolution`、`OpticFinalResolution`、`_CamTex`、`SSAAOpticCurrent` 等）均直接取自**本机安装的 SPT 4.1.x 实际 `Assembly-CSharp.dll`** 反编译结果，而非公开资料。

===============================================================

# Compatibility and Limitations

## Interaction with Other Mods

- **PiP-Disabler** (`com.fiodor.pipdisabler`): it is a **postfix gate** on `SetResolution` (it neither rewrites the argument nor the field), so it coexists with this plugin; suppressed scoped rendering does not trigger this plugin's conflict retry.
- **Fontaine's FOV Fix / Amands's Graphics**: they patch targets (`OpticComponentUpdater`, etc.) that do not overlap this plugin, so there is no direct conflict.
- **DERP (Dynamic External Resolution Patch)**: DERP acts on the **main camera** only — it patches `OpticSight.OnEnable` / `OpticSight.OnDisable` / `Player.FirearmController.ChangeAimingMode` and calls `SSAAImpl.Switch` / `SetAntiAliasing` / `SetFSR2` / `SetFSR3` on `EFT.CameraControl.CameraManager`. It neither patches `OpticCameraManager.SetResolution` nor touches `OpticFinalResolution`, so it shares no patch target or state with this plugin and the two can run together; their effects stack: the exterior is lowered by DERP and the in-scope image by this plugin. Two caveats: (1) DERP itself black-flickers on scope transition under DLSS/FSR (the official advice is TAA + Sampling Downgrade); (2) when both lower resolution, the lens area is degraded twice and usually looks softer than with either alone.
- **DLSS / FSR**: this plugin **does not switch upscaler modes**; however, changing the scoped RT size while an upscaler is active may cause a one-frame flicker — turn off "瞄准中立即应用" to avoid it. SPT officially documents a black flicker when switching modes under DLSS/FSR.

## Known Limitations and Risks

- **No independent in-scope upscaler setting**: the game has a single `SSAA`/`SSAAImpl` on the main camera and the scoped image is part of it; a truly independent in-scope FSR/DLSS would require attaching a second component, which this plugin does not do.
- **Changing resolution while scoped may flash for one frame** because the RenderTexture is destroyed and recreated; turn off "瞄准中立即应用" to avoid it.
- **Texture aliasing becomes more visible at low in-scope resolutions**: the scope camera's mipmap bias is only set once in `Init()`, and the plugin does not compensate.
- **Possible conflict with other mods that patch `SetResolution`**: if a third-party prefix rewrites the same argument, the plugin detects the target not taking effect and pauses retries until you change the target, avoiding repeated RenderTexture rebuilds.
- **Version sensitive**: it depends on specific types and methods and may break after an EFT update.
- **CI limitation**: a client plugin must reference proprietary assemblies, which public runners cannot build, so the repository keeps only `test.yml` (pure-logic unit tests); release packages are produced locally with `scripts/release.sh`.

## Source of the Symbols

> Every game symbol used here (`OpticCameraManager.SetResolution`, `OpticFinalResolution`, `_CamTex`, `SSAAOpticCurrent`, …) was taken directly from decompiling the **SPT 4.1.x `Assembly-CSharp.dll` installed on this machine**, not from public sources.
