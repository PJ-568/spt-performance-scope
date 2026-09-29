using EFT.CameraControl;
using HarmonyLib;

namespace PerformanceScope
{
    /// <summary>
    /// 覆盖游戏传入的镜内分辨率参数，并把新建的镜内管理器交给服务订阅退出事件。游戏自身 Init() 调用
    /// <c>SetResolution(OpticFinalResolution)</c>（初值 1024）时，prefix 会直接改成目标值，
    /// 从而避免先建 1024 再重建造成的闪烁。
    /// Overrides the scope resolution argument passed by the game and hands the newly created optic camera manager
    /// to the service so it can subscribe to its scope-exit event. When the game's own Init() calls
    /// <c>SetResolution(OpticFinalResolution)</c> (initial value 1024), the prefix replaces it with the target value,
    /// avoiding the flicker caused by creating 1024 first and then rebuilding.
    /// </summary>
    [HarmonyPatch(typeof(OpticCameraManager), nameof(OpticCameraManager.SetResolution))]
    internal static class SetResolutionPatch
    {
        // HarmonyWrapSafe：prefix 抛异常时包裹并跳过原方法而非中断游戏 Init 流程。
        // HarmonyWrapSafe: if the prefix throws, wrap it and skip the original method instead of aborting the game's Init flow.
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Prefix(OpticCameraManager __instance, ref int resolution)
        {
            OpticQualityService service = OpticQualityService.Instance;
            if (service == null)
            {
                // 插件尚未初始化完成，保持游戏原值透传。
                // The plugin is not initialized yet; pass the game's value through.
                return;
            }

            resolution = service.ComputeTarget();
            service.OnOpticCameraManagerReady(__instance);
        }
    }
}
