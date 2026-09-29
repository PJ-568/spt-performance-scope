# 实机验证

## 启动自检

游戏启动后，`BepInEx/LogOutput.log` 应出现：

```
[Info   :PerformanceScope] PerformanceScope 已加载（在 ConfigurationManager 菜单中调参）
[Info   :PerformanceScope] 镜内分辨率 1024 → 720
```

第二行示例表示镜内分辨率已从游戏默认 `1024` 切到 `720`。

## 验证步骤

1. 在 ConfigurationManager 里开启“启用日志”。
2. 进入战局，按住右键进入瞄具。
3. 打开 ConfigurationManager（SPT 中默认 F12），把“取值方式”切到“屏幕高度比例”或“绝对像素”。
4. 观察 `BepInEx/LogOutput.log` 的 `镜内分辨率 1024 → …` 与镜内画质/帧数变化。
5. 关闭“瞄准中立即应用”后瞄准时改值，确认改动在退出镜内后才生效。

## 排查建议

- 所有运行日志（包括常规、诊断、警告与错误）都写入 `BepInEx/LogOutput.log`，遇到异常先看该文件。
- 若插件检测到目标值未生效，会进入 `Conflict` 状态。该状态由冲突锁定产生（见[兼容性与限制](compatibility.md)），此时插件会暂停重试，直到你更改目标值。

===============================================================

# In-Game Verification

## Startup Self-Check

On startup `BepInEx/LogOutput.log` should show:

```
[Info   :PerformanceScope] PerformanceScope 已加载（在 ConfigurationManager 菜单中调参）
[Info   :PerformanceScope] 镜内分辨率 1024 → 720
```

The second line means the scope resolution has switched from the game default `1024` to `720`.

## Verification Steps

1. Enable "启用日志 | Enable Logging" in ConfigurationManager.
2. Enter a raid and aim down sights through a magnified optic.
3. Open ConfigurationManager (default F12 in SPT) and switch "取值方式 | Resolution Mode" to "屏幕高度比例" or "绝对像素".
4. Watch `BepInEx/LogOutput.log` for `镜内分辨率 1024 → …`, and check the scoped image quality / frame rate.
5. Turn off "瞄准中立即应用 | Apply While Scoped", change the value while scoped, and confirm the change only applies after you unscope.

## Troubleshooting

- Every runtime log (info, verbose, warning and error) is written to `BepInEx/LogOutput.log`; check that file first when something looks wrong.
- If the plugin detects that the target value is not taking effect, it enters the `Conflict` state. That state is produced by the conflict lock (see [Compatibility and limitations](compatibility.md)); the plugin then pauses retries until you change the target value.
