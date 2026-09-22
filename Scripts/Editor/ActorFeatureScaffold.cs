using System;
using System.IO;

using UnityEditor;
using UnityEngine;

namespace Abc.Unity.Editor
{
    internal static class ActorFeatureScaffold
    {
        public static void Generate()
        {
            var args = Environment.GetCommandLineArgs();
            var feature = ReadArgument(args, "-abcFeature");
            var namespaceName = ReadArgument(args, "-abcNamespace", ActorProjectSettings.instance.FeatureNamespace);
            var folder = ReadArgument(args, "-abcOutput", ActorProjectSettings.instance.FeatureFolder);
            var files = ActorFeatureTemplates.Build(feature, namespaceName, true, true, true, true, true);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            ActorFeatureWriter.Write(projectRoot, folder, feature, namespaceName, files);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"ABC_FEATURE_GENERATED: {files.Count} editable files in {folder}");
        }

        private static string ReadArgument(string[] args, string key, string fallback = null)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] != key)
                    continue;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    return args[i + 1];
                throw new ArgumentException($"Missing value for argument: {key}");
            }

            if (fallback != null)
                return fallback;
            throw new ArgumentException($"Missing required argument: {key}");
        }
    }
}
