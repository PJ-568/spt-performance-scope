using PerformanceScope.Core;
using Xunit;

namespace PerformanceScope.Tests
{
    public class ScopeMipMathTests
    {
        [Fact]
        public void 游戏默认模式_不覆盖()
        {
            Assert.False(ScopeMipMath.TryResolve(ScopeSettingMode.GameDefault, 5f, out float bias));
            Assert.Equal(0f, bias);
        }

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(3f, 3f)]
        [InlineData(-1.5f, -1.5f)]
        public void 自定义模式_返回夹取后的值(float input, float expected)
        {
            Assert.True(ScopeMipMath.TryResolve(ScopeSettingMode.Custom, input, out float bias));
            Assert.Equal(expected, bias, 3);
        }

        [Theory]
        [InlineData(99f, ScopeMipMath.MaxBias)]
        [InlineData(-99f, ScopeMipMath.MinBias)]
        public void 自定义模式_越界被夹取(float input, float expected)
        {
            Assert.True(ScopeMipMath.TryResolve(ScopeSettingMode.Custom, input, out float bias));
            Assert.Equal(expected, bias, 3);
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void 非法值_回退默认偏差(float input)
        {
            Assert.Equal(ScopeMipMath.DefaultCustomBias, ScopeMipMath.ClampBias(input), 3);
        }

        [Fact]
        public void 默认自定义偏差在合法区间内()
        {
            Assert.InRange(ScopeMipMath.DefaultCustomBias, ScopeMipMath.MinBias, ScopeMipMath.MaxBias);
        }
    }
}
