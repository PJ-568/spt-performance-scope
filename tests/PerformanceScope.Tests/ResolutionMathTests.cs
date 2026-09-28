using PerformanceScope.Core;
using Xunit;

namespace PerformanceScope.Tests
{
    public class ResolutionMathTests
    {
        [Theory]
        [InlineData(0.5f, 1440, 720)]
        [InlineData(0.7f, 1080, 756)]
        [InlineData(1.0f, 1080, 1080)]
        [InlineData(0.666f, 1440, 959)] // 959.04 → 四舍五入 959 · 959.04 → rounded to 959
        public void ScreenHeightRatio_按屏幕高度换算(float ratio, int screenHeight, int expected)
        {
            int actual = ResolutionMath.Resolve(OpticResolutionMode.ScreenHeightRatio, screenHeight, ratio, 0);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AbsolutePixels_直接取给定值()
        {
            int actual = ResolutionMath.Resolve(OpticResolutionMode.AbsolutePixels, 1080, 0f, 512);
            Assert.Equal(512, actual);
        }

        [Fact]
        public void GameDefault_返回默认值()
        {
            int actual = ResolutionMath.Resolve(OpticResolutionMode.GameDefault, 1080, 0.5f, 512);
            Assert.Equal(ResolutionMath.GameDefaultResolution, actual);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(0.001f)]
        [InlineData(99f)]
        public void 非法比例_回退默认值(float ratio)
        {
            int actual = ResolutionMath.Resolve(OpticResolutionMode.ScreenHeightRatio, 1080, ratio, 0);
            Assert.Equal(ResolutionMath.GameDefaultResolution, actual);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void 非法像素_回退默认值(int pixels)
        {
            int actual = ResolutionMath.Resolve(OpticResolutionMode.AbsolutePixels, 1080, 0f, pixels);
            Assert.Equal(ResolutionMath.GameDefaultResolution, actual);
        }

        [Theory]
        [InlineData(1, ResolutionMath.MinResolution)]
        [InlineData(99999, ResolutionMath.MaxResolution)]
        public void 结果被夹取到合法区间(int input, int expected)
        {
            int actual = ResolutionMath.Resolve(OpticResolutionMode.AbsolutePixels, 1080, 0f, input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void 屏幕高度为零时_回退默认值()
        {
            int actual = ResolutionMath.Resolve(OpticResolutionMode.ScreenHeightRatio, 0, 0.5f, 0);
            Assert.Equal(ResolutionMath.GameDefaultResolution, actual);
        }
    }
}
