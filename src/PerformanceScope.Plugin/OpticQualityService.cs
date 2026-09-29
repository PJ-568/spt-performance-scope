using System;
using System.Reflection;
using EFT.CameraControl;
using HarmonyLib;
using PerformanceScope.Core;
using UnityEngine;

namespace PerformanceScope
{
    /// <summary>
    /// 镜内画质的决策与应用，完全事件驱动，不做任何每帧轮询：
    /// 分辨率与贴图 mip 在 SetResolution 的 prefix（战局初始化）中定位，
    /// 镜内细节在 CopyComponentFromOptic 的 postfix 中按瞄具重设，
    /// 配置变更由插件防抖后调用 <see cref="TryApply"/>。
    /// Decision and application of in-scope quality, fully event-driven with no per-frame polling:
    /// resolution and texture mip are reached from the SetResolution prefix at raid init, in-scope
    /// detail is re-applied per optic in the CopyComponentFromOptic postfix, and config changes are
    /// debounced by the plugin, which then calls <see cref="TryApply"/>.
    /// </summary>
    internal sealed class OpticQualityService
    {
        /// <summary>镜内相机上要按自定义模式关闭的组件对应的私有字段名。</summary>
        /// <summary>Private field names of the optic-camera components disabled in custom detail mode.</summary>
        private static readonly string[] DetailFieldNames =
        {
            "bloomOptimized",
            "ultimateBloom",
            "chromaticAberration",
            "fisheye"
        };

        /// <summary>与 DetailFieldNames 对应的可读名称，用于日志。</summary>
        /// <summary>Human-readable names matching DetailFieldNames, used for logging.</summary>
        private static readonly string[] DetailLabels =
        {
            "泛光 / bloom",
            "终极泛光 / ultimate bloom",
            "色散 / chromatic aberration",
            "鱼眼 / fisheye"
        };

        private readonly FieldInfo[] _detailFields = new FieldInfo[DetailFieldNames.Length];

        private readonly bool[] _detailOriginal = new bool[DetailFieldNames.Length];

        private FieldInfo _cachedOpticIdField;

        private OpticCameraManager _hookedManager;

        private int _failedTarget = int.MinValue;

        private int _detailOpticId = int.MinValue;

        private readonly bool[] _detailOverridden = new bool[DetailFieldNames.Length];

        private int _detailAppliedMask;

        /// <summary>
        /// 当前服务实例；插件未加载时为 null，补丁据此透传原值。
        /// Current service instance; null when the plugin is not loaded, in which case patches pass the original value through.
        /// </summary>
        public static OpticQualityService Instance { get; set; }

        /// <summary>
        /// 计算当前配置对应的目标边长。模组关闭时返回游戏默认值，从而让游戏恢复其原生 1024。
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
        /// 取消镜内管理器的订阅。
        /// Remove the optic camera manager subscription.
        /// </summary>
        public void Unhook()
        {
            if (_hookedManager == null)
            {
                return;
            }

            try
            {
                _hookedManager.OnOpticDisabled -= OnOpticDisabled;
            }
            catch (Exception e)
            {
                Log.Error("解除镜内事件订阅失败 | Failed to unsubscribe optic events: " + e);
            }

            _hookedManager = null;
        }

        /// <summary>
        /// 由 SetResolution 的 prefix 在游戏创建镜内管理器时调用：按实例幂等地订阅退出事件，并应用贴图 mip 覆盖。
        /// Called by the SetResolution prefix when the game creates the optic camera manager: subscribes
        /// (idempotently per instance) to its scope-exit event and applies the texture mip override.
        /// </summary>
        public void OnOpticCameraManagerReady(OpticCameraManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (!ReferenceEquals(_hookedManager, manager))
            {
                if (_hookedManager != null)
                {
                    _hookedManager.OnOpticDisabled -= OnOpticDisabled;
                }

                _hookedManager = manager;
                _hookedManager.OnOpticDisabled += OnOpticDisabled;
            }

            ApplyMip(manager);
        }

        /// <summary>
        /// 由 CopyComponentFromOptic 的 postfix 调用：按瞄具缓存游戏的原始值，再应用细节覆盖。
        /// Called by the CopyComponentFromOptic postfix: cache the game's original values per optic, then apply detail overrides.
        /// </summary>
        public void OnOpticComponentsCopied(OpticComponentUpdater updater)
        {
            if (updater == null)
            {
                return;
            }

            int opticId = ReadOpticId(updater);
            if (opticId != _detailOpticId)
            {
                // 游戏内部有 cachedOpticId 守卫：同一瞄具重复进镜时不会重写这些组件。
                // 若此时重新缓存，会把我们关掉的值当成原始值，因此只在瞄具变化时缓存。
                // The game guards this with cachedOpticId: re-entering with the same optic skips the writes.
                // Re-caching here would record our disabled values as the originals, so cache only on change.
                _detailOpticId = opticId;
                CaptureDetailOriginals(updater);
            }

            ApplyDetails(updater);
        }

        /// <summary>
        /// 在安全时机同步分辨率、贴图 mip 与镜内细节；不满足条件时留给后续事件处理。
        /// Sync resolution, texture mip and in-scope detail at a safe moment; otherwise leave it to later events.
        /// </summary>
        public void TryApply()
        {
            OpticCameraManager manager = GetOpticCameraManager();
            if (manager == null || manager.Camera == null)
            {
                // 未进入战局：战局开始时 Init() 的 prefix 会按目标分辨率建 RT 并应用 mip。
                // Not in a raid yet: the prefix on Init() will build the RT at the target size and apply the mip override.
                Log.Verbose("镜内相机尚未就绪，交由战局初始化处理。 | Optic camera not ready; deferring to raid init.");
                return;
            }

            ApplyMip(manager);
            ApplyDetails(manager._updater);

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

        private void OnOpticDisabled()
        {
            TryApply();
        }

        /// <summary>
        /// 自定义模式下覆盖镜内相机的贴图 mip 偏差。
        /// In custom mode, override the optic camera's texture mip bias.
        /// </summary>
        private void ApplyMip(OpticCameraManager manager)
        {
            ScopeSettingMode mode = PluginConfig.MipMode?.Value ?? ScopeSettingMode.GameDefault;
            float custom = PluginConfig.MipBias?.Value ?? ScopeMipMath.DefaultCustomBias;
            if (!ScopeMipMath.TryResolve(mode, custom, out float bias))
            {
                return;
            }

            if (manager._streamingController == null)
            {
                return;
            }

            if (Math.Abs(manager._streamingController.streamingMipmapBias - bias) < 0.0001f)
            {
                return;
            }

            manager._streamingController.streamingMipmapBias = bias;
            Log.Info($"镜内 mip 偏差 → {bias} | Scope mip bias → {bias}");
        }

        /// <summary>
        /// 记录游戏刚写入的细节开关原始值。
        /// Record the original values of the detail toggles as just written by the game.
        /// </summary>
        private void CaptureDetailOriginals(OpticComponentUpdater updater)
        {
            for (int i = 0; i < _detailFields.Length; i++)
            {
                Behaviour behaviour = GetDetailBehaviour(updater, i);
                _detailOriginal[i] = behaviour != null && behaviour.enabled;
            }
        }

        /// <summary>
        /// 按逐项开关应用镜内细节：勾选则关闭对应组件，取消勾选则还原为游戏原始值。
        /// Apply the in-scope detail toggles: a checked effect is disabled, an unchecked one is restored to the game's original value.
        /// </summary>
        private void ApplyDetails(OpticComponentUpdater updater)
        {
            if (updater == null)
            {
                return;
            }

            for (int i = 0; i < _detailFields.Length; i++)
            {
                Behaviour behaviour = GetDetailBehaviour(updater, i);
                if (behaviour == null)
                {
                    continue;
                }

                if (IsDetailDisabled(i))
                {
                    if (behaviour.enabled)
                    {
                        behaviour.enabled = false;
                    }

                    _detailOverridden[i] = true;
                }
                else if (_detailOverridden[i])
                {
                    // 只还原「我们关掉过」的项，避免在从未覆盖时误写游戏的默认值。
                    // Restore only effects we actually disabled, so a never-overridden state never writes the game's defaults.
                    behaviour.enabled = _detailOriginal[i];
                    _detailOverridden[i] = false;
                }
            }

            int mask = DesiredDetailMask();
            if (mask != _detailAppliedMask)
            {
                _detailAppliedMask = mask;
                Log.Info(mask == 0
                    ? "镜内细节已还原为游戏默认 | In-scope detail restored to game default"
                    : $"镜内细节覆盖已更新：关闭 {DescribeDisabled(mask)} | In-scope detail overrides updated: {DescribeDisabled(mask)}");
            }
        }

        /// <summary>
        /// 逐项开关是否勾选（勾选表示关闭该效果）。
        /// Whether an individual detail toggle is checked (checked means the effect is disabled).
        /// </summary>
        private static bool IsDetailDisabled(int index)
        {
            switch (index)
            {
                case 0:
                    return PluginConfig.DisableBloom?.Value == true;
                case 1:
                    return PluginConfig.DisableUltimateBloom?.Value == true;
                case 2:
                    return PluginConfig.DisableChromaticAberration?.Value == true;
                case 3:
                    return PluginConfig.DisableFisheye?.Value == true;
                default:
                    return false;
            }
        }

        private static int DesiredDetailMask()
        {
            int mask = 0;
            for (int i = 0; i < DetailFieldNames.Length; i++)
            {
                if (IsDetailDisabled(i))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static string DescribeDisabled(int mask)
        {
            string text = string.Empty;
            for (int i = 0; i < DetailLabels.Length; i++)
            {
                if ((mask & (1 << i)) == 0)
                {
                    continue;
                }

                if (text.Length > 0)
                {
                    text += "、";
                }

                text += DetailLabels[i];
            }

            return text;
        }

        private Behaviour GetDetailBehaviour(OpticComponentUpdater updater, int index)
        {
            FieldInfo field = _detailFields[index]
                ?? (_detailFields[index] = AccessTools.Field(typeof(OpticComponentUpdater), DetailFieldNames[index]));
            return field == null ? null : field.GetValue(updater) as Behaviour;
        }

        private int ReadOpticId(OpticComponentUpdater updater)
        {
            if (_cachedOpticIdField == null)
            {
                _cachedOpticIdField = AccessTools.Field(typeof(OpticComponentUpdater), "cachedOpticId");
            }

            if (_cachedOpticIdField == null)
            {
                return 0;
            }

            object value = _cachedOpticIdField.GetValue(updater);
            return value is int id ? id : 0;
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
