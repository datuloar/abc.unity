using System;

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

using Abc.Unity.Editor;

namespace Abc.Unity.Tests
{
    public sealed class ActorBlueprintAuthoringTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            var name = "AbcBlueprintTest-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name);
            _folder = "Assets/" + name;
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(_folder);

        [Test]
        public void RemovingSharedProviderPreservesItsAsset()
        {
            var blueprint = CreateBlueprint();
            var provider = ScriptableObject.CreateInstance<SerializedDataProvider>();
            var providerPath = _folder + "/Shared.asset";
            AssetDatabase.CreateAsset(provider, providerPath);
            var serialized = Assign(blueprint, provider, 1);

            ActorBlueprintAuthoring.RemoveProvider(serialized, serialized.FindProperty("_data"), 0);

            Assert.That(blueprint.Data.Count, Is.Zero);
            Assert.That(AssetDatabase.LoadAssetAtPath<SerializedDataProvider>(providerPath), Is.SameAs(provider));
        }

        [Test]
        public void RemovingDuplicateEmbeddedReferencePreservesRemainingReference()
        {
            var blueprint = CreateBlueprint();
            var provider = ScriptableObject.CreateInstance<SerializedDataProvider>();
            AssetDatabase.AddObjectToAsset(provider, blueprint);
            var serialized = Assign(blueprint, provider, 2);

            ActorBlueprintAuthoring.RemoveProvider(serialized, serialized.FindProperty("_data"), 0);

            Assert.That(provider == null, Is.False);
            Assert.That(blueprint.Data.Count, Is.EqualTo(1));
            Assert.That(blueprint.Data[0], Is.SameAs(provider));

            ActorBlueprintAuthoring.RemoveProvider(serialized, serialized.FindProperty("_data"), 0);
            Assert.That(provider == null, Is.True);
        }

        private ActorBlueprint CreateBlueprint()
        {
            var blueprint = ScriptableObject.CreateInstance<ActorBlueprint>();
            AssetDatabase.CreateAsset(blueprint, _folder + "/Blueprint.asset");
            return blueprint;
        }

        private static SerializedObject Assign(ActorBlueprint blueprint, ActorDataProviderBase provider, int count)
        {
            var serialized = new SerializedObject(blueprint);
            var data = serialized.FindProperty("_data");
            data.arraySize = count;
            for (var i = 0; i < count; i++)
                data.GetArrayElementAtIndex(i).objectReferenceValue = provider;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return serialized;
        }
    }
}
