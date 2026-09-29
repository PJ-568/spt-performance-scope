using EFT.CameraControl;
using HarmonyLib;

namespace PerformanceScope
{
    /// <summary>
    /// 游戏按瞄具写完镜内相机组件后，交给服务应用「镜内细节」覆盖。
    /// 该方法在每次进入镜内时都会被调用（游戏内部再用 cachedOpticId 守卫实际写入）。
    /// After the game has configured the optic-camera components for the current optic, hand control to the
    /// service so it can apply the in-scope detail overrides. This runs on every scope entry (the game itself
    /// guards the actual writes with cachedOpticId).
    /// </summary>
    [HarmonyPatch(typeof(OpticComponentUpdater), nameof(OpticComponentUpdater.CopyComponentFromOptic))]
    internal static class CopyComponentFromOpticPatch
    {
        // HarmonyWrapSafe：postfix 抛异常时包裹并跳过，不中断游戏进镜流程。
        // HarmonyWrapSafe: if the postfix throws, wrap it instead of aborting the game's scope-enter flow.
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Postfix(OpticComponentUpdater __instance)
        {
            OpticQualityService.Instance?.OnOpticComponentsCopied(__instance);
        }
    }
}
