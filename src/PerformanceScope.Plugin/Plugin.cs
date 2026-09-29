using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PerformanceScope
{
    /// <summary>
    /// 插件入口：初始化配置、日志、Harmony 补丁与分辨率服务。
    /// 所有调参通过 BepInEx ConfigurationManager 在游戏内完成（SPT 中默认 F12，可在其配置中改键）；
    /// 配置变更在此防抖后交给服务应用，瞄具退出等事件由服务自行订阅。本插件不提供自定义 UI 或热键。
    /// Plugin entry point: initializes config, logging, Harmony patches and the resolution service.
    /// All tweaking is done in-game via BepInEx ConfigurationManager (F12 by default in SPT; rebindable in its config);
    /// config changes are debounced here before being handed to the service, while events such as scope exit are
    /// subscribed by the service itself. This plugin offers no custom UI or hotkey.
    /// </summary>
    [BepInPlugin("com.pj568.performancescope", "PerformanceScope", "0.1.1")]
    public class PerformanceScopePlugin : BaseUnityPlugin
    {
        private const string HarmonyId = "com.pj568.performancescope";

        /// <summary>
        /// 配置变更的防抖时长（秒）。拖动 ConfigurationManager 滑块时几乎每帧都在写值，
        /// 若每次都重建 RenderTexture 会连续卡顿，因此等停手后再应用一次。
        /// Debounce window (seconds) for config changes. Dragging a ConfigurationManager slider writes the value on
        /// almost every frame, and rebuilding the RenderTexture each time would stutter, so apply once after it settles.
        /// </summary>
        private const float ApplyDebounceSeconds = 0.25f;

        private readonly WaitForSecondsRealtime _debounceDelay = new WaitForSecondsRealtime(ApplyDebounceSeconds);

        private Harmony _harmony;

        private Coroutine _pendingApply;

        private void Awake()
        {
            Log.Init(Logger);
            PluginConfig.Init(Config);
            ApplyHarmonyPatches();

            OpticQualityService service = new OpticQualityService();
            OpticQualityService.Instance = service;

            // 事件驱动：订阅配置变更（防抖后应用），不做任何每帧轮询。
            // Event-driven: subscribe to config changes (applied after a debounce); no per-frame polling.
            if (PluginConfig.File != null)
            {
                PluginConfig.File.SettingChanged += OnSettingChanged;
            }

            Log.Info("PerformanceScope 已加载 | loaded（在 ConfigurationManager 菜单中调参 / configure via ConfigurationManager）");
        }

        private void OnDestroy()
        {
            if (PluginConfig.File != null)
            {
                PluginConfig.File.SettingChanged -= OnSettingChanged;
            }

            if (_pendingApply != null)
            {
                StopCoroutine(_pendingApply);
                _pendingApply = null;
            }

            OpticQualityService.Instance?.Unhook();

            if (_harmony == null)
            {
                return;
            }

            try
            {
                _harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                Log.Error("卸载 Harmony 补丁失败 | Failed to unpatch Harmony patches: " + e);
            }
        }

        /// <summary>
        /// 配置变更回调：重启防抖计时，停手后再应用一次，避免拖动滑块期间反复重建 RenderTexture。
        /// Config change callback: restart the debounce timer and apply once after the changes settle, avoiding
        /// repeated RenderTexture rebuilds while a slider is being dragged.
        /// </summary>
        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (_pendingApply != null)
            {
                StopCoroutine(_pendingApply);
            }

            _pendingApply = StartCoroutine(ApplyAfterDebounce());
        }

        private IEnumerator ApplyAfterDebounce()
        {
            yield return _debounceDelay;
            _pendingApply = null;
            OpticQualityService.Instance?.TryApply();
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll(typeof(SetResolutionPatch));
            }
            catch (Exception e)
            {
                Log.Error("应用 Harmony 补丁失败 | Failed to apply Harmony patches: " + e);
            }
        }
    }
}
