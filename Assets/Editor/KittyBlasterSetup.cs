using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Gorilla Company > 7. Add Kitty Blaster (Mod Only)
// Saves the Kitty Blaster prefab to Assets/Weapons/Mod and hooks it into the Mod Panel
// (ModPowers.kittyBlasterPrefab + ModPowers.spawnables) in the open scene.
// It is NOT added to the weapon racks or the 100-weapon list.
public static class KittyBlasterSetup
{
    public const string Folder = "Assets/Weapons/Mod";
    public const string PrefabPath = Folder + "/Kitty Blaster.prefab";

    [MenuItem("Gorilla Company/7. Add Kitty Blaster (Mod Only)")]
    static void AddToScene()
    {
        var prefab = BuildPrefab();
        var mod = Object.FindAnyObjectByType<ModPowers>();
        if (mod == null)
        {
            EditorUtility.DisplayDialog("Gorilla Company",
                "Kitty Blaster prefab saved to " + PrefabPath + ".\n\n" +
                "But this scene has no Mod Panel yet. Run Gorilla Company > 3. Add Mod Panel, then run this again.", "OK");
            return;
        }

        Undo.RecordObject(mod, "Add Kitty Blaster");
        mod.kittyBlasterPrefab = prefab;
        var list = new List<GameObject>(mod.spawnables ?? new GameObject[0]);
        if (!list.Contains(prefab)) list.Add(prefab);
        mod.spawnables = list.ToArray();
        EditorUtility.SetDirty(mod);

        GorillaCombatSetup.Finish(mod.gameObject,
            "Kitty Blaster is ready (mod only)!\n\n" +
            "Save the scene with Ctrl+S, press Play, unlock the Mod Panel and press KITTY.\n" +
            "No headset? Type the code + Enter, then press K (spawn) or L (launch a kitten).");
    }

    // Also works from the command line: -executeMethod KittyBlasterSetup.BuildPrefabCommandLine
    public static void BuildPrefabCommandLine()
    {
        BuildPrefab();
        Debug.Log("[Kitty] Saved " + PrefabPath);
    }

    public static GameObject BuildPrefab()
    {
        EnsureFolder("Assets/Weapons");
        EnsureFolder(Folder);
        var root = KittyBlaster.Create(Vector3.zero, Quaternion.identity);
        try
        {
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
