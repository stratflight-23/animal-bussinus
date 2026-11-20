using System;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

// Gorilla Company > Extras > Set Up OpenXR (PC + Quest)
// Does the "XR Plug-in Management" clicking for you:
//   - PC (Windows/Linux) tab : OpenXR on, Oculus Touch + Meta Quest Touch Plus controller profiles on
//   - Android (Quest) tab    : OpenXR on, "Meta Quest" feature group on, same controller profiles,
//                              plus Quest-friendly Android settings (ARM64, IL2CPP, Android 12L / API 32+)
// Safe to run again. The Android part only fully works once Android Build Support is installed in Unity Hub,
// so run this again after installing it.
public static class VRSetup
{
    const string MetaQuestFeatureSet = "com.unity.openxr.featureset.metaquest";

    [MenuItem("Gorilla Company/Extras/Set Up OpenXR (PC + Quest)")]
    static void SetUpMenu()
    {
        string report = SetUp();
        EditorUtility.DisplayDialog("Gorilla Company", report, "OK");
    }

    // command line: -executeMethod VRSetup.SetUpFromCommandLine
    public static void SetUpFromCommandLine() => Debug.Log("[VR Setup]\n" + SetUp());

    static string SetUp()
    {
        string report = "";
        report += Configure(BuildTargetGroup.Standalone, false);
        report += Configure(BuildTargetGroup.Android, true);

        // Quest-friendly Android player settings
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel32)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        report += "Android: IL2CPP + ARM64, minimum Android API 32.\n";

        AssetDatabase.SaveAssets();
        return report;
    }

    static string Configure(BuildTargetGroup group, bool quest)
    {
        string name = group == BuildTargetGroup.Android ? "Quest (Android)" : "PC";
        try
        {
            // 1. XR Plug-in Management: use OpenXR and start XR when the game starts
            var perTarget = GetOrCreatePerTargetSettings();
            if (!perTarget.HasManagerSettingsForBuildTarget(group))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var general = perTarget.SettingsForBuildTarget(group);
            general.InitManagerOnStart = true;
            bool loaderOk = XRPackageMetadataStore.AssignLoader(general.Manager, typeof(OpenXRLoader).FullName, group);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.Manager);

            // 2. OpenXR features
            if (quest)
            {
                var set = OpenXRFeatureSetManager.GetFeatureSetWithId(group, MetaQuestFeatureSet);
                if (set != null)
                {
                    set.isEnabled = true;
                    OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(group);
                }
            }

            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            int enabled = 0;
            if (settings != null)
            {
                foreach (var f in settings.GetFeatures())
                {
                    string type = f.GetType().Name;
                    bool want = type == "OculusTouchControllerProfile" || type == "MetaQuestTouchPlusControllerProfile" ||
                                (quest && type == "MetaQuestFeature");
                    if (want && !f.enabled) f.enabled = true;
                    if (want) enabled++;
                    EditorUtility.SetDirty(f);
                }
                EditorUtility.SetDirty(settings);
            }
            return $"{name}: OpenXR {(loaderOk ? "ON" : "(already on)")}, {enabled} Quest feature(s)/controller profile(s) on.\n";
        }
        catch (Exception e)
        {
            return $"{name}: could not finish ({e.Message}). If this is the Android one, install Android Build Support and run this again.\n";
        }
    }

    static XRGeneralSettingsPerBuildTarget GetOrCreatePerTargetSettings()
    {
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
        if (perTarget != null) return perTarget;

        if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
        perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
        AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        return perTarget;
    }
}
