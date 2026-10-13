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
        public const string Version = "0.2.0";
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
                string zip = Zip(folder, ProductName + ".app");
                EditorUtility.RevealInFinder(zip ?? app);
            }
            else
            {
                Debug.LogError($"[Build] {report.summary.result}: {report.summary.totalErrors} errors. See the Console above.");
            }
        }

        // The app packed for the GitHub release (ditto keeps the app's links and signature intact).
        private static string Zip(string folder, string appName)
        {
            string zip = $"TheQuietWitness-v{Version}-macOS.zip";
            string full = Path.Combine(folder, zip);
            if (File.Exists(full)) File.Delete(full);
            try
            {
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/usr/bin/ditto",
                    Arguments = $"-c -k --sequesterRsrc --keepParent \"{appName}\" \"{zip}\"",
                    WorkingDirectory = Path.GetFullPath(folder),
                    UseShellExecute = false,
                });
                process.WaitForExit();
                if (process.ExitCode != 0 || !File.Exists(full)) throw new System.Exception("ditto exit code " + process.ExitCode);
                Debug.Log($"[Build] Packed for the release: {Path.GetFullPath(full)} ({new FileInfo(full).Length / (1024 * 1024)} MB)");
                return full;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Build] Could not zip the app ({e.Message}). Right-click the .app > Compress instead.");
                return null;
            }
        }
    }
}
