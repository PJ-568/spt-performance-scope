using System;

namespace PerformanceScope.Core
{
    /// <summary>
    /// 镜内贴图 mip 偏差的换算：纯函数，无游戏依赖，便于单元测试。
    /// Pure helpers for the in-scope texture mip bias; no game dependency, so it is unit-testable.
    /// </summary>
    public static class ScopeMipMath
    {
        /// <summary>允许的最小偏差。</summary>
        /// <summary>Smallest allowed bias.</summary>
        public const float MinBias = -2f;

        /// <summary>允许的最大偏差。</summary>
        /// <summary>Largest allowed bias.</summary>
        public const float MaxBias = 8f;

        /// <summary>自定义模式的默认偏差。</summary>
        /// <summary>Default bias for custom mode.</summary>
        public const float DefaultCustomBias = 3f;

        /// <summary>把偏差夹取到 [MinBias, MaxBias]；非法值回退到 DefaultCustomBias。</summary>
        /// <summary>Clamp a bias to [MinBias, MaxBias]; invalid values fall back to DefaultCustomBias.</summary>
        public static float ClampBias(float bias)
        {
            if (float.IsNaN(bias) || float.IsInfinity(bias))
            {
                return DefaultCustomBias;
            }

            if (bias < MinBias)
            {
                return MinBias;
            }

            return bias > MaxBias ? MaxBias : bias;
        }

        /// <summary>
        /// 解析要写入镜内相机的偏差。仅自定义模式返回 true；
        /// 游戏默认模式返回 false，表示「不要覆盖游戏的既有值」。
        /// Resolve the bias to write to the optic camera. Returns true only in custom mode;
        /// game-default mode returns false, meaning "do not override the game's existing value".
        /// </summary>
        public static bool TryResolve(ScopeSettingMode mode, float customBias, out float bias)
        {
            if (mode == ScopeSettingMode.Custom)
            {
                bias = ClampBias(customBias);
                return true;
            }

            bias = 0f;
            return false;
        }
    }
}
