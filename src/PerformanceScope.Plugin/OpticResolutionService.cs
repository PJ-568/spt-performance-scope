using System;
using BepInEx.Configuration;
using EFT.CameraControl;
using PerformanceScope.Core;
using UnityEngine;

namespace PerformanceScope
{
    /// <summary>
    /// 镜内 PiP 分辨率的决策与应用，完全事件驱动，不做任何每帧轮询：
    /// 配置变更来自 ConfigFile.SettingChanged，瞄具退出来自 OpticCameraManager.OnOpticDisabled，
    /// 战局开始则由 SetResolution 的 prefix 直接覆盖（无需运行时补偿）。
    /// Decision and application of the in-scope PiP resolution, fully event-driven with no per-frame polling:
    /// config changes come from ConfigFile.SettingChanged, scope exit from OpticCameraManager.OnOpticDisabled,
    /// and raid start is covered directly by the SetResolution prefix.
    /// </summary>
    internal sealed class OpticResolutionService
    {
        private OpticCameraManager _hookedManager;

        private int _failedTarget = int.MinValue;

        /// <summary>
        /// 当前服务实例；插件未加载时为 null，补丁据此透传原值。
        /// Current service instance; null when the plugin is not loaded, in which case the patch passes the original value through.
        /// </summary>
        public static OpticResolutionService Instance { get; set; }

        /// <summary>
        /// 计算当前配置对应的目标边长。模组关闭时返回游戏默认值，
        /// 从而让游戏恢复其原生 1024。
        /// Compute the target edge length for the current config. Returns the game default when the mod is off,
        /// so the game falls back to its native 1024.
        /// </summary>
        public int ComputeTarget()
        {
            if (PluginConfig.EnableMod?.Value != true)
            {
                return ResolutionMath.GameDefaultResolution;
            }

            return ResolutionMath.Resolve(
                PluginConfig.Mode.Value,
                Screen.height,
                PluginConfig.ScreenHeightRatio.Value,
                PluginConfig.AbsolutePixels.Value);
        }

        /// <summary>
        /// 订阅配置变更（ConfigFile 级事件）。
        /// Subscribe to config changes (ConfigFile-level event).
        /// </summary>
        public void HookSettings()
        {
            if (PluginConfig.File == null)
            {
                return;
            }

            PluginConfig.File.SettingChanged += OnSettingChanged;
        }

        /// <summary>
        /// 取消全部订阅（配置与镜内管理器）。
        /// Remove every subscription (config and optic camera manager).
        /// </summary>
        public void Unhook()
        {
            if (PluginConfig.File != null)
            {
                PluginConfig.File.SettingChanged -= OnSettingChanged;
            }

            if (_hookedManager != null)
            {
                try
                {
                    _hookedManager.OnOpticDisabled -= OnOpticDisabled;
                }
                catch (Exception e)
                {
                    Log.Error("解除镜内事件订阅失败：" + e + " | Failed to unsubscribe optic events: " + e);
                }

                _hookedManager = null;
            }
        }

        /// <summary>
        /// 由 SetResolution 的 prefix 在游戏创建镜内管理器时调用，按实例幂等地订阅其退出事件。
        /// Called by the SetResolution prefix when the game creates the optic camera manager; subscribes
        /// (idempotently per instance) to its scope-exit event.
        /// </summary>
        public void OnOpticCameraManagerReady(OpticCameraManager manager)
        {
            if (manager == null || ReferenceEquals(_hookedManager, manager))
            {
                return;
            }

            if (_hookedManager != null)
            {
                _hookedManager.OnOpticDisabled -= OnOpticDisabled;
                _hookedManager = null;
            }

            _hookedManager = manager;
            _hookedManager.OnOpticDisabled += OnOpticDisabled;
        }

        /// <summary>
        /// 在安全时机把配置分辨率同步给游戏；不满足条件时留给后续事件处理。
        /// Sync the configured resolution to the game at a safe moment; otherwise leave it to later events.
        /// </summary>
        public void TryApply()
        {
            OpticCameraManager manager = GetOpticCameraManager();
            if (manager == null || manager.Camera == null)
            {
                // 未进入战局：战局开始时 Init() 的 prefix 会直接按目标分辨率建 RT。
                // Not in a raid yet: the prefix on Init() will create the RT at the target size.
                Log.Verbose("镜内相机尚未就绪，交由战局初始化处理。 | Optic camera not ready; deferring to raid init.");
                return;
            }

            int target = ComputeTarget();
            if (target == _failedTarget)
            {
                return;
            }

            OpticApplyAction action = OpticResolutionDecision.Decide(
                manager.OpticFinalResolution,
                target,
                manager.IsAnyOpticCameraRendering,
                PluginConfig.ApplyWhileScoped?.Value == true);

            if (action != OpticApplyAction.Apply)
            {
                // Defer：瞄准中且配置禁止立即应用，等 OnOpticDisabled；None：无需动作。
                // Defer: scoped and immediate apply is disabled, wait for OnOpticDisabled. None: nothing to do.
                return;
            }

            Apply(manager, target);
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            TryApply();
        }

        private void OnOpticDisabled()
        {
            TryApply();
        }

        /// <summary>
        /// 安全地调用游戏接口重建镜内 RenderTexture。
        /// Rebuild the in-scope RenderTexture through the game API, safely.
        /// </summary>
        private void Apply(OpticCameraManager manager, int target)
        {
            int old = manager.OpticFinalResolution;
            if (old == target)
            {
                _failedTarget = int.MinValue;
                return;
            }

            try
            {
                manager.SetResolution(target);
            }
            catch (Exception e)
            {
                Log.Error($"设置镜内分辨率失败（{old} → {target}）：{e} | Failed to set scope resolution ({old} → {target}): {e}");
                return;
            }

            Log.Info($"镜内分辨率 {old} → {target} | Scope resolution {old} → {target}");

            if (manager.OpticFinalResolution != target)
            {
                // 第三方 prefix/postfix 可能改写了结果；锁定目标以避免反复重建 RenderTexture。
                // A third-party prefix/postfix may have rewritten the result; lock the target to avoid repeated rebuilds.
                _failedTarget = target;
                Log.Warn($"镜内分辨率未生效（仍为 {manager.OpticFinalResolution}），可能存在冲突模组；已暂停重试直至目标变化。 | Scope resolution did not stick (still {manager.OpticFinalResolution}); possible mod conflict, retries paused until the target changes.");
                return;
            }

            _failedTarget = int.MinValue;
        }

        private static OpticCameraManager GetOpticCameraManager()
        {
            // 读取静态字段而非 Instance 属性：避免触发 CameraManager 的懒创建。
            // Read the static field rather than the Instance property to avoid forcing lazy creation.
            CameraManager manager = CameraManager.instance;
            return manager == null ? null : manager.OpticCameraManager;
        }
    }
}
