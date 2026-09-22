using System;

using NUnit.Framework;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class FastListTests
    {
        [Test]
        public void AddInsertAndRemovePreserveOrder()
        {
            var list = new ActorFastList<int>();

            list.Add(1);
            list.Add(3);
            list.Insert(1, 2);
            list.RemoveAt(0);

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo(2));
            Assert.That(list[1], Is.EqualTo(3));
        }

        [Test]
        public void RemoveAtSwapBackUsesConstantTimeLayout()
        {
            var list = new ActorFastList<int>();

            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.RemoveAtSwapBack(0);

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo(3));
            Assert.That(list[1], Is.EqualTo(2));
        }

        [Test]
        public void InvalidIndexesAndCapacityAreRejected()
        {
            var list = new ActorFastList<int>();
            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(2, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = 0);
        }

        [Test]
        public void ClearReleasesReferences()
        {
            var list = new ActorFastList<object>();
            list.Add(new object());

            list.Clear();

            Assert.That(list.Count, Is.Zero);
            Assert.That(list.Span.Length, Is.Zero);
        }
    }
}
