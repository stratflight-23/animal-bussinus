using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Adds "Gorilla Company > Build" menu items, and the same builds can be run from the terminal
// (Unity must be closed first):
//
//   ~/Unity/Hub/Editor/6000.6.10f1/Editor/Unity -batchmode -quit -projectPath "$HOME/animal bussinus" \
//       -executeMethod BuildGame.BuildQuest -logFile -
//
//   ... -executeMethod BuildGame.BuildLinux -logFile -
//   ... -executeMethod BuildGame.BuildWebXR -logFile -      (Quest web browser version)
//
// Builds use every scene that is ticked in File > Build Profiles (Build Settings) and go into the
// Builds/ folder, which git ignores.
public static class BuildGame
{
    const string QuestPath = "Builds/Quest/GorillaCompany.apk";
    const string LinuxPath = "Builds/Linux/GorillaCompany.x86_64";
    const string WebXRPath = "Builds/WebXR";

    [MenuItem("Gorilla Company/Build/Quest (Android APK)")]
    public static void BuildQuest()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Fail("Android Build Support is not installed for this Unity version.\n" +
                 "Unity Hub > Installs > 6000.6.10f1 > gear icon > Add modules > tick Android Build Support " +
                 "(with OpenJDK and Android SDK & NDK Tools).");
            return;
        }

        // Meta Quest needs 64-bit ARM, which needs the IL2CPP scripting backend.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        Build(BuildTarget.Android, QuestPath);
    }

    [MenuItem("Gorilla Company/Build/Linux Desktop")]
    public static void BuildLinux()
    {
        Build(BuildTarget.StandaloneLinux64, LinuxPath);
    }

    // The Quest web-browser version (WebXR). Makes a folder with index.html you can put on a website;
    // open it in the Quest Browser and press the VR button. Uses the WebXR Export add-on.
    [MenuItem("Gorilla Company/Build/Quest Browser (WebXR)")]
    public static void BuildWebXR()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Fail("Web Build Support is not installed for this Unity version (Unity Hub > Installs > Add modules).");
            return;
        }
        Debug.Log("[BuildGame] " + VRSetup.ConfigureWebXR().Trim());

        PlayerSettings.WebGL.template = "PROJECT:WebXRFullView2020";   // the page with the VR button
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;             // works on any web host
        PlayerSettings.WebGL.dataCaching = true;

        // WebXR needs "Gamma" colors. Only for this build - the Quest app / PC stay on Linear.
        var oldColorSpace = PlayerSettings.colorSpace;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        try
        {
            Build(BuildTarget.WebGL, WebXRPath);
        }
        finally
        {
            PlayerSettings.colorSpace = oldColorSpace;
            AssetDatabase.SaveAssets();
        }
    }

    static void Build(BuildTarget target, string outputPath)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Fail("No scenes are ticked in Build Settings. Run Gorilla Company > 2. Build VR Test Scene first, " +
                 "or tick a scene in File > Build Profiles.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None,
        };

        Debug.Log($"[BuildGame] Building {target} with scenes: {string.Join(", ", scenes)}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            string fullPath = Path.GetFullPath(outputPath);
            Debug.Log($"[BuildGame] Build succeeded: {fullPath} ({summary.totalSize / (1024 * 1024)} MB, " +
                      $"{summary.totalTime.TotalSeconds:F0}s)");
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(fullPath);
        }
        else
        {
            Fail($"Build {summary.result} with {summary.totalErrors} error(s). Check the Console / log above.");
        }
    }

    // Shows the problem in the editor, or exits with an error code when run from the terminal.
    static void Fail(string message)
    {
        Debug.LogError("[BuildGame] " + message);
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
        else
            EditorUtility.DisplayDialog("Build failed", message, "OK");
    }
}
