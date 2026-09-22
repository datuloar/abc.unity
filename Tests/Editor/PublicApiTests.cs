using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class PublicApiTests
    {
        [Test]
        public void RuntimeHostsRemainInternalAndRegistryMutationIsHidden()
        {
            Assert.That(typeof(ActorRegistry).IsPublic, Is.True);
            Assert.That(typeof(ActorRegistryHost).IsNotPublic, Is.True);
            Assert.That(typeof(ActorServiceHost<>).IsNotPublic, Is.True);

            var publicMethods = typeof(ActorRegistry)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(method => method.Name)
                .ToArray();

            Assert.That(publicMethods, Does.Contain(nameof(ActorRegistry.Get)));
            Assert.That(publicMethods, Does.Contain(nameof(ActorRegistry.TryGet)));
            Assert.That(publicMethods, Does.Not.Contain("Add"));
            Assert.That(publicMethods, Does.Not.Contain("Remove"));
            Assert.That(publicMethods, Does.Not.Contain("CleanUp"));
        }
    }
}
