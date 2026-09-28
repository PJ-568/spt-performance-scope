using System;
using EFT.CameraControl;
using PerformanceScope.Core;
using UnityEngine;

namespace PerformanceScope
{
    /// <summary>
    /// 镜内 PiP 分辨率的决策与应用。每帧由插件调用 <see cref="Tick"/>，
    /// 根据配置与当前游戏状态决定何时调用 <see cref="OpticCameraManager.SetResolution"/>。
    /// 普通类，由插件在 Awake 中创建并注册到 <see cref="Instance"/>。
    /// </summary>
    internal sealed class OpticResolutionService
    {
        private bool _loggedNoCamera;

        private int _pendingTarget = ResolutionMath.GameDefaultResolution;

        private int _conflictTarget = int.MinValue;

        /// <summary>当前服务实例；插件未加载时为 null，补丁据此透传原值。</summary>
        public static OpticResolutionService Instance { get; set; }

        /// <summary>最近一次解析出的目标边长，供叠加显示。</summary>
        public int LastTarget { get; private set; } = ResolutionMath.GameDefaultResolution;

        /// <summary>是否有因瞄准中而推迟应用的目标。</summary>
        public bool PendingApply { get; private set; }

        /// <summary>是否因第三方改写导致目标无法生效而暂停重试。</summary>
        public bool ConflictLocked { get; private set; }

        /// <summary>
        /// 计算当前配置对应的目标边长。模组关闭时返回游戏默认值，
        /// 从而让游戏恢复其原生 1024。
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

        /// <summary>每帧调用：在相机就绪时把配置分辨率同步给游戏。</summary>
        public void Tick()
        {
            OpticCameraManager ocm = GetOpticCameraManager();
            if (ocm == null || ocm.Camera == null)
            {
                if (!_loggedNoCamera)
                {
                    _loggedNoCamera = true;
                    Log.Verbose("镜内相机尚未就绪，暂不应用。");
                }

                return;
            }

            _loggedNoCamera = false;

            int target = ComputeTarget();
            LastTarget = target;

            if (ConflictLocked)
            {
                // 仅在目标变化（例如玩家改了配置）时解除锁定，避免每帧重建。
                if (target == _conflictTarget)
                {
                    return;
                }

                ConflictLocked = false;
            }

            OpticApplyAction action = OpticResolutionDecision.Decide(
                ocm.OpticFinalResolution,
                target,
                ocm.IsAnyOpticCameraRendering,
                PluginConfig.ApplyWhileScoped?.Value == true);

            switch (action)
            {
                case OpticApplyAction.Apply:
                    Apply(ocm, target);
                    break;

                case OpticApplyAction.Defer:
                    if (!PendingApply || _pendingTarget != target)
                    {
                        _pendingTarget = target;
                        PendingApply = true;
                        Log.Info($"镜内瞄准中，推迟应用分辨率 {ocm.OpticFinalResolution} → {target}");
                    }

                    break;

                default:
                    PendingApply = false;
                    break;
            }
        }

        /// <summary>安全地调用游戏接口重建镜内 RenderTexture。</summary>
        private void Apply(OpticCameraManager ocm, int target)
        {
            int old = ocm.OpticFinalResolution;
            if (old == target)
            {
                PendingApply = false;
                LastTarget = target;
                return;
            }

            try
            {
                ocm.SetResolution(target);
            }
            catch (Exception e)
            {
                Log.Error($"设置镜内分辨率失败（{old} → {target}）：{e}");
                return;
            }

            PendingApply = false;
            LastTarget = target;
            Log.Info($"镜内分辨率 {old} → {target}");

            if (ocm.OpticFinalResolution != target)
            {
                // 第三方 prefix/postfix 可能改写了结果；锁定以避免每帧重建 RenderTexture。
                ConflictLocked = true;
                _conflictTarget = target;
                Log.Warn($"镜内分辨率未生效（仍为 {ocm.OpticFinalResolution}），可能存在冲突模组；已暂停重试直至目标变化。");
            }
        }

        private static OpticCameraManager GetOpticCameraManager()
        {
            // 读取静态字段而非 Instance 属性：避免在菜单等场景中触发 CameraManager 的懒创建。
            CameraManager manager = CameraManager.instance;
            return manager == null ? null : manager.OpticCameraManager;
        }
    }
}
