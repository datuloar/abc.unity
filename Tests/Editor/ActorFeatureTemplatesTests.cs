using NUnit.Framework;

using Abc.Unity.Editor;

namespace Abc.Unity.Tests
{
    public sealed class ActorFeatureTemplatesTests
    {
        [Test]
        public void DefaultFeatureIsDeterministicAndComplete()
        {
            var first = ActorFeatureTemplates.Build("Movement", "Game.Features", true, true, false, false, true);
            var second = ActorFeatureTemplates.Build("Movement", "Game.Features", true, true, false, false, true);

            Assert.That(first, Has.Count.EqualTo(4));
            Assert.That(second, Has.Count.EqualTo(first.Count));
            Assert.That(first[0].Name, Is.EqualTo("MovementData.cs"));
            Assert.That(first[1].Name, Is.EqualTo("MovementDataProvider.cs"));
            Assert.That(first[2].Name, Is.EqualTo("MovementBehaviour.cs"));
            Assert.That(first[3].Name, Is.EqualTo("MovementBehaviourProvider.cs"));

            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Name, Is.EqualTo(first[i].Name));
                Assert.That(second[i].Content, Is.EqualTo(first[i].Content));
                Assert.That(first[i].Content, Does.Contain("namespace Game.Features"));
                Assert.That(first[i].Content, Does.Not.Contain("//"));
            }
        }

        [Test]
        public void CommandAndQueryActionUseTypedContracts()
        {
            var files = ActorFeatureTemplates.Build("Damage", "Game.Combat", true, true, true, true, false);

            Assert.That(files, Has.Count.EqualTo(4));
            Assert.That(FindContent(files, "DamageBehaviour.cs"), Does.Contain("IActorCommandListener<DamageCommand>"));
            Assert.That(FindContent(files, "DamageBehaviour.cs"), Does.Contain("Owner.GetData<DamageData>()"));
            Assert.That(FindContent(files, "DamageCommand.cs"), Does.Contain("IActorCommand"));
            Assert.That(FindContent(files, "DamageAction.cs"), Does.Contain("IActorQueryAction<DamageData>"));
        }

        [Test]
        public void QueryActionRequiresData()
        {
            var files = ActorFeatureTemplates.Build("Signal", "Game.Signals", false, false, true, true, true);

            Assert.That(files, Has.Count.EqualTo(1));
            Assert.That(files[0].Name, Is.EqualTo("SignalCommand.cs"));
        }

        private static string FindContent(System.Collections.Generic.IReadOnlyList<ActorFeatureFile> files, string name)
        {
            for (var i = 0; i < files.Count; i++)
            {
                if (files[i].Name == name)
                    return files[i].Content;
            }

            Assert.Fail($"Generated file {name} was not found.");
            return null;
        }
    }
}
