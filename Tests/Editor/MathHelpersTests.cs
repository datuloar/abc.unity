using System;

using NUnit.Framework;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class MathHelpersTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 4)]
        [TestCase(1025, 2048)]
        public void NextPowerOfTwoReturnsExpectedValue(int value, int expected) =>
            Assert.That(MathHelpers.NextPowerOf2(value), Is.EqualTo(expected));

        [Test]
        public void NextPowerOfTwoRejectsInvalidRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MathHelpers.NextPowerOf2(-1));
            Assert.Throws<OverflowException>(() => MathHelpers.NextPowerOf2((1 << 30) + 1));
        }

        [TestCase(0, -1)]
        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(7, 2)]
        [TestCase(8, 3)]
        public void FastLogTwoReturnsFloor(int value, int expected) =>
            Assert.That(MathHelpers.FastLog2(value), Is.EqualTo(expected));

        [Test]
        public void FastOperationsValidateTheirContracts()
        {
            Assert.That(MathHelpers.FastMod(15, 8), Is.EqualTo(7));
            Assert.That(MathHelpers.FastPowDiv(32, 3), Is.EqualTo(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => MathHelpers.FastMod(1, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => MathHelpers.FastPowDiv(1, 31));
        }
    }
}
