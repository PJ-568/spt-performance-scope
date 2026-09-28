namespace PerformanceScope.Core
{
    /// <summary>一次决策应执行的动作。</summary>
    public enum OpticApplyAction
    {
        /// <summary>无需动作。</summary>
        None = 0,

        /// <summary>立即应用目标分辨率。</summary>
        Apply = 1,

        /// <summary>推迟到退出镜内后再应用。</summary>
        Defer = 2
    }

    /// <summary>
    /// 镜内分辨率应用的纯决策逻辑：输入游戏当前状态，输出该做什么。
    /// 与游戏 API 解耦，便于单元测试。
    /// </summary>
    public static class OpticResolutionDecision
    {
        /// <summary>
        /// 决策。
        /// </summary>
        /// <param name="current">游戏当前镜内分辨率。</param>
        /// <param name="target">配置目标分辨率。</param>
        /// <param name="isScoped">是否正在镜内瞄准。</param>
        /// <param name="applyWhileScoped">是否允许瞄准中立即应用。</param>
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
