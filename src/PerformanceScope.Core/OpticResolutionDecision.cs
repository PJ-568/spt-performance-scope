namespace PerformanceScope.Core
{
    /// <summary>
    /// 一次决策应执行的动作。
    /// The action a single decision should perform.
    /// </summary>
    public enum OpticApplyAction
    {
        /// <summary>
        /// 无需动作。
        /// No action needed.
        /// </summary>
        None = 0,

        /// <summary>
        /// 立即应用目标分辨率。
        /// Apply the target resolution immediately.
        /// </summary>
        Apply = 1,

        /// <summary>
        /// 推迟到退出镜内后再应用。
        /// Defer until after leaving the scope.
        /// </summary>
        Defer = 2
    }

    /// <summary>
    /// 镜内分辨率应用的纯决策逻辑：输入游戏当前状态，输出该做什么。
    /// 与游戏 API 解耦，便于单元测试。
    /// Pure decision logic for applying the scope resolution: takes the current game state and returns what to do.
    /// Decoupled from the game API, easy to unit test.
    /// </summary>
    public static class OpticResolutionDecision
    {
        /// <summary>
        /// 决策。
        /// Decides.
        /// </summary>
        /// <param name="current">游戏当前镜内分辨率 / Current in-game scope resolution.</param>
        /// <param name="target">配置目标分辨率 / Configured target resolution.</param>
        /// <param name="isScoped">是否正在镜内瞄准 / Whether the player is currently scoped.</param>
        /// <param name="applyWhileScoped">是否允许瞄准中立即应用 / Whether applying while scoped is allowed.</param>
        public static OpticApplyAction Decide(int current, int target, bool isScoped, bool applyWhileScoped)
        {
            if (current == target)
            {
                return OpticApplyAction.None;
            }

            return isScoped && !applyWhileScoped ? OpticApplyAction.Defer : OpticApplyAction.Apply;
        }
    }
}
