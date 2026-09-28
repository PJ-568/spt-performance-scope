namespace PerformanceScope.Core
{
    /// <summary>
    /// 镜内（PiP）渲染分辨率的取值方式。
    /// </summary>
    public enum OpticResolutionMode
    {
        /// <summary>使用游戏默认值（当前版本为 1024）。</summary>
        GameDefault = 0,

        /// <summary>按屏幕高度的比例取值：round(屏幕高度 × 比例)。</summary>
        ScreenHeightRatio = 1,

        /// <summary>取固定的绝对像素值。</summary>
        AbsolutePixels = 2
    }
}
