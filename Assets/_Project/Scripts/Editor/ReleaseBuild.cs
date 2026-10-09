using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QuietWitness.EditorTools
{
    // Builds the macOS app for a release.
    // Menu: Tools > The Quiet Witness > Build > macOS
    public static class ReleaseBuild
    {
        public const string Version = "0.1.0";
        private const string ProductName = "The Quiet Witness";

        [MenuItem("Tools/The Quiet Witness/Build/macOS")]
        private static void BuildMac()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = "sidequestioon";
            PlayerSettings.bundleVersion = Version;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Build", "No scenes in Build Profiles > Scene List.", "OK");
                return;
            }

            string folder = Path.Combine("Builds", "v" + Version);
            Directory.CreateDirectory(folder);
            string app = Path.Combine(folder, ProductName + ".app");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = app,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            });

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] Done: {Path.GetFullPath(app)} ({report.summary.totalSize / (1024 * 1024)} MB)");
                EditorUtility.RevealInFinder(app);
            }
            else
            {
                Debug.LogError($"[Build] {report.summary.result}: {report.summary.totalErrors} errors. See the Console above.");
            }
        }
    }
}
