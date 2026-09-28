using System;
using BepInEx;
using HarmonyLib;

namespace PerformanceScope
{
    /// <summary>
    /// 插件入口：初始化配置、日志、Harmony 补丁与分辨率服务。
    /// 所有调参通过 BepInEx ConfigurationManager 在游戏内完成（SPT 中默认 F12，可在其配置中改键），
    /// 服务层每帧读取配置并即时生效；本插件不提供自定义 UI 或热键。
    /// Plugin entry point: initializes config, logging, Harmony patches and the resolution service.
    /// All tweaking is done in-game via BepInEx ConfigurationManager (F12 by default in SPT; rebindable in its config);
    /// the service reads config every frame and applies it immediately. This plugin offers no custom UI or hotkey.
    /// </summary>
    [BepInPlugin("com.pj568.performancescope", "PerformanceScope", "0.1.0")]
    public class PerformanceScopePlugin : BaseUnityPlugin
    {
        private const string HarmonyId = "com.pj568.performancescope";

        private Harmony _harmony;

        private void Awake()
        {
            Log.Init(Logger);
            PluginConfig.Init(Config);
            ApplyHarmonyPatches();

            OpticResolutionService.Instance = new OpticResolutionService();

            Log.Info("PerformanceScope 已加载 | loaded（在 ConfigurationManager 菜单中调参 / configure via ConfigurationManager）");
        }

        private void Update()
        {
            // 每帧同步：ConfigurationManager 里改动配置后立即生效，并处理瞄准中的延迟应用。
            // Per-frame sync: apply ConfigurationManager changes immediately and handle
            // deferred in-scope applies.
            OpticResolutionService.Instance?.Tick();
        }

        private void OnDestroy()
        {
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
