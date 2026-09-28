using System;

namespace PerformanceScope.Core
{
    /// <summary>
    /// 镜内渲染分辨率换算：纯函数，无游戏依赖，便于单元测试。
    /// </summary>
    public static class ResolutionMath
    {
        /// <summary>游戏当前版本的镜内方形 RenderTexture 默认边长。</summary>
        public const int GameDefaultResolution = 1024;

        /// <summary>允许的最小边长。</summary>
        public const int MinResolution = 64;

        /// <summary>允许的最大边长。</summary>
        public const int MaxResolution = 4096;

        /// <summary>允许的最小比例。</summary>
        public const float MinRatio = 0.05f;

        /// <summary>允许的最大比例。</summary>
        public const float MaxRatio = 4f;

        /// <summary>把边长夹取到 [MinResolution, MaxResolution]。</summary>
        public static int ClampResolution(int value)
        {
            if (value < MinResolution)
            {
                return MinResolution;
            }

            return value > MaxResolution ? MaxResolution : value;
        }

        /// <summary>比例是否有效（有限且落在允许区间内）。</summary>
        public static bool IsValidRatio(float ratio)
        {
            return !float.IsNaN(ratio) && !float.IsInfinity(ratio) && ratio >= MinRatio && ratio <= MaxRatio;
        }

        /// <summary>
        /// 计算镜内渲染分辨率边长。非法或缺失输入一律回退到 <paramref name="gameDefault"/>。
        /// </summary>
        /// <param name="mode">取值方式。</param>
        /// <param name="screenHeight">当前屏幕高度（像素）。</param>
        /// <param name="gameDefault">游戏默认边长，默认 <see cref="GameDefaultResolution"/>。</param>
        /// <param name="screenHeightRatio">屏幕高度比例（仅 <see cref="OpticResolutionMode.ScreenHeightRatio"/> 用）。</param>
        /// <param name="absolutePixels">绝对像素边长（仅 <see cref="OpticResolutionMode.AbsolutePixels"/> 用）。</param>
        public static int Resolve(
            OpticResolutionMode mode,
            int screenHeight,
            float screenHeightRatio,
            int absolutePixels,
            int gameDefault = GameDefaultResolution)
        {
            switch (mode)
            {
                case OpticResolutionMode.ScreenHeightRatio:
                    if (IsValidRatio(screenHeightRatio) && screenHeight > 0)
                    {
                        return ClampResolution(
                            (int)Math.Round(screenHeight * (double)screenHeightRatio, MidpointRounding.AwayFromZero));
                    }

                    return ClampResolution(gameDefault);

                case OpticResolutionMode.AbsolutePixels:
                    return absolutePixels > 0 ? ClampResolution(absolutePixels) : ClampResolution(gameDefault);

                default:
                    return ClampResolution(gameDefault);
            }
        }
    }
}
