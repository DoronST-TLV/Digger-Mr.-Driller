using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Strata.EditorTools
{
    /// <summary>
    /// Menu: Strata → Build Windows / Build Android APK.
    /// Batch: -executeMethod Strata.EditorTools.BuildScript.BuildWindows (or BuildAndroid)
    /// </summary>
    public static class BuildScript
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Strata/Build Windows")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Strata.exe");
        }

        [MenuItem("Strata/Build Android APK")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/Strata.apk");
        }

        private static void Build(BuildTarget target, string outputPath)
        {
            SceneBuilder.ApplyPlayerSettings();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Strata] Build succeeded: {outputPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
                return;
            }
            Debug.LogError($"[Strata] Build FAILED for {target}: {report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
