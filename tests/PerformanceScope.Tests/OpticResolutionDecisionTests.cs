using PerformanceScope.Core;
using Xunit;

namespace PerformanceScope.Tests
{
    public class OpticResolutionDecisionTests
    {
        [Fact]
        public void 当前值与目标一致_不动作()
        {
            Assert.Equal(
                OpticApplyAction.None,
                OpticResolutionDecision.Decide(1024, 1024, isScoped: false, applyWhileScoped: true));
        }

        [Fact]
        public void 未瞄准_立即应用()
        {
            Assert.Equal(
                OpticApplyAction.Apply,
                OpticResolutionDecision.Decide(1024, 720, isScoped: false, applyWhileScoped: false));
        }

        [Fact]
        public void 瞄准中且允许立即应用_立即应用()
        {
            Assert.Equal(
                OpticApplyAction.Apply,
                OpticResolutionDecision.Decide(1024, 720, isScoped: true, applyWhileScoped: true));
        }

        [Fact]
        public void 瞄准中且禁止立即应用_推迟()
        {
            Assert.Equal(
                OpticApplyAction.Defer,
                OpticResolutionDecision.Decide(1024, 720, isScoped: true, applyWhileScoped: false));
        }

        [Fact]
        public void 一致优先于推迟()
        {
            Assert.Equal(
                OpticApplyAction.None,
                OpticResolutionDecision.Decide(720, 720, isScoped: true, applyWhileScoped: false));
        }
    }
}
