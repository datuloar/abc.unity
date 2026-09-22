using System;
using System.IO;

using NUnit.Framework;
using UnityEditor;

using Abc.Unity.Editor;

namespace Abc.Unity.Tests
{
    public sealed class ActorFeatureWriterTests
    {
        private string _root;

        [SetUp]
        public void SetUp() => _root = Path.Combine(Path.GetTempPath(), "AbcScaffold-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [TestCase("Assets/Combat")]
        [TestCase("Assets/_Project/Develop/My Company/My Game/Runtime/Combat")]
        [TestCase("Assets/Игра/Features")]
        public void WritesReadableDeterministicSource(string outputFolder)
        {
            var files = ActorFeatureTemplates.Build("Health", "Game.Combat", true, true, true, true, true);
            ActorFeatureWriter.Write(_root, outputFolder, "Health", "Game.Combat", files);

            foreach (var file in files)
                Assert.That(File.ReadAllText(Path.Combine(_root, outputFolder, file.Name)), Is.EqualTo(file.Content));
        }

        [Test]
        public void ExistingSourcePreventsAllWrites()
        {
            var files = ActorFeatureTemplates.Build("Health", "Game.Combat", true, true, true, true, true);
            var folder = Path.Combine(_root, "Assets/Combat");
            Directory.CreateDirectory(folder);
            var existing = Path.Combine(folder, "HealthBehaviour.cs");
            File.WriteAllText(existing, "user source");

            Assert.Throws<InvalidOperationException>(() => ActorFeatureWriter.Write(_root, "Assets/Combat", "Health", "Game.Combat", files));
            Assert.That(Directory.GetFiles(folder), Has.Length.EqualTo(1));
            Assert.That(File.ReadAllText(existing), Is.EqualTo("user source"));
        }

        [TestCase("../Outside")]
        [TestCase("Assets/../../Outside")]
        [TestCase("Assets/./Combat")]
        [TestCase("Assets//Combat")]
        [TestCase("Packages/Combat")]
        [TestCase("Assets/Combat:Stream")]
        public void InvalidFoldersAreRejected(string folder)
        {
            var files = ActorFeatureTemplates.Build("Health", "Game.Combat", true, false, false, false, false);
            Assert.That(ActorFeatureWriter.Validate(_root, folder, "Health", "Game.Combat", files), Is.Not.Null);
            Assert.That(Directory.Exists(_root), Is.False);
        }

        [TestCase("class", "Game.Combat")]
        [TestCase("Health", "Game.class")]
        [TestCase("Health", "Game..Combat")]
        [TestCase("__arglist", "Game.Combat")]
        public void InvalidIdentifiersAreRejected(string feature, string namespaceName)
        {
            var files = ActorFeatureTemplates.Build(feature, namespaceName, true, false, false, false, false);
            Assert.That(ActorFeatureWriter.Validate(_root, "Assets/Combat", feature, namespaceName, files), Is.Not.Null);
        }

        [Test]
        public void ExistingMetaFileIsNeverReused()
        {
            var files = ActorFeatureTemplates.Build("Health", "Game.Combat", true, false, false, false, false);
            var folder = Path.Combine(_root, "Assets/Combat");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "HealthData.cs.meta"), "existing guid");

            Assert.Throws<InvalidOperationException>(() => ActorFeatureWriter.Write(_root, "Assets/Combat", "Health", "Game.Combat", files));
            Assert.That(Directory.GetFiles(folder), Has.Length.EqualTo(1));
        }

        [Test]
        public void ProjectFolderDefaultFollowsAssetGuidAfterMove()
        {
            var name = "AbcFolderTest-" + Guid.NewGuid().ToString("N");
            var original = "Assets/" + name;
            var moved = original + "-Moved";
            var guid = AssetDatabase.CreateFolder("Assets", name);
            try
            {
                Assert.That(AssetDatabase.MoveAsset(original, moved), Is.Empty);
                Assert.That(ActorProjectSettings.ResolveFolder(original, guid), Is.EqualTo(moved));
            }
            finally
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }
}
