using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Awake.Tests
{
    [TestClass]
    public class AwakePolicyTests
    {
        [TestMethod]
        public void CalculateIdleMinutes_UsesLastInputAndAddsOneMinute()
        {
            Assert.AreEqual(9, AwakePolicy.CalculateIdleMinutes(600000, 90000));
        }

        [TestMethod]
        public void CalculateIdleMinutes_HandlesTickCountWraparound()
        {
            Assert.AreEqual(11, AwakePolicy.CalculateIdleMinutes(100000, 4294457296));
        }

        [TestMethod]
        public void ShouldDeactivate_DisablesAfterConfiguredPeriod()
        {
            Assert.IsTrue(AwakePolicy.ShouldDeactivate(30, 31, false, false));
            Assert.IsFalse(AwakePolicy.ShouldDeactivate(30, 30, false, false));
        }

        [TestMethod]
        public void ShouldDeactivate_ForeverNeverExpires()
        {
            Assert.IsFalse(AwakePolicy.ShouldDeactivate(-1, 10000, false, false));
        }

        [TestMethod]
        public void ShouldDeactivate_RespectsLockScreenSetting()
        {
            Assert.IsTrue(AwakePolicy.ShouldDeactivate(-1, 1, true, true));
            Assert.IsFalse(AwakePolicy.ShouldDeactivate(-1, 1, true, false));
        }
    }
}
