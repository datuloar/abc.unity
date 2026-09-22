using System;
using System.IO;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Abc.Unity.Samples.NetworkSimulation.Editor
{
    internal static class NetworkSimulationBuild
    {
        [MenuItem("GameObject/ABC Samples/Server Physics Host", false, 10)]
        private static void CreateHost()
        {
            var host = new GameObject("ABC Server Physics");
            Undo.RegisterCreatedObjectUndo(host, "Create ABC server physics host");
            Undo.AddComponent<ServerPhysicsHost>(host);
            Selection.activeGameObject = host;
        }

        public static void BuildWindowsMono()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run this build through the documented Unity batchmode command.");
            var output = GetOutputPath();
            var directory = Path.GetDirectoryName(output);
            if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length != 0)
                throw new IOException("Choose an empty output directory for the server sample build.");

            var previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scenePath = "Assets/ABCServerBuild-" + Guid.NewGuid().ToString("N") + ".unity";
            try
            {
                SceneManager.SetActiveScene(scene);
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.reflectionIntensity = 0f;
                new GameObject("ABC Server Physics").AddComponent<ServerPhysicsHost>();
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                    throw new IOException("The temporary server scene could not be saved.");
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("The server sample player build failed.");
                Debug.Log($"ABC_SERVER_BUILD_PASSED warnings={report.summary.totalWarnings} output={output}");
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previousBackend);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(scenePath);
            }
        }

        private static string GetOutputPath()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < arguments.Length; i++)
            {
                if (arguments[i] == "-abcBuildPath")
                    return Path.Combine(Path.GetFullPath(arguments[i + 1]), "ABC Server.exe");
            }
            throw new ArgumentException("Supply -abcBuildPath with an empty output directory.");
        }
    }
}
