using System.IO;
using UnityEditor;
using UnityEngine;

// Gorilla Company > 4. Generate All Weapons
// Makes a WeaponData file (Assets/Weapons/Data) and a prefab (Assets/Weapons/Prefabs) for all 100 weapons.
// Safe to run again: it updates the existing files instead of making duplicates.
// NOTE: running it again resets stats to the catalog values (WeaponCatalog.cs) - change stats there to keep them.
public static class WeaponGenerator
{
    public const string DataFolder = "Assets/Weapons/Data";
    public const string PrefabFolder = "Assets/Weapons/Prefabs";

    [MenuItem("Gorilla Company/4. Generate All Weapons")]
    public static void GenerateAll()
    {
        var defs = WeaponCatalog.All();
        if (defs.Count != 100)
            Debug.LogWarning($"[Weapons] The catalog has {defs.Count} weapons (expected 100).");

        EnsureFolder("Assets/Weapons");
        EnsureFolder(DataFolder);
        EnsureFolder(PrefabFolder);

        int made = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            string lastCategory = null;
            int indexInCategory = 0;

            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                EditorUtility.DisplayProgressBar("Gorilla Company", "Making " + def.name, (float)i / defs.Count);

                if (def.category != lastCategory) { lastCategory = def.category; indexInCategory = 0; }
                else indexInCategory++;

                var data = CreateOrUpdateData(def, i, indexInCategory);
                AssetDatabase.StopAssetEditing(); // the prefab needs the data asset to exist on disk
                BuildPrefab(data);
                AssetDatabase.StartAssetEditing();
                made++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[Weapons] Generated {made} weapons in {DataFolder} and {PrefabFolder}.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Gorilla Company",
                $"Made {made} weapons!\n\nRecipe cards: {DataFolder}\nPrefabs: {PrefabFolder}\n\n" +
                "Next: Gorilla Company > 5. Add Weapon Range to put them all in your scene.", "OK");
    }

    static WeaponData CreateOrUpdateData(WeaponCatalog.Def def, int catalogIndex, int indexInCategory)
    {
        string path = $"{DataFolder}/{FileName(def.name)}.asset";
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        bool isNew = data == null;
        if (isNew) data = ScriptableObject.CreateInstance<WeaponData>();

        data.weaponName = def.name;
        data.category = def.category;
        data.description = def.description;
        data.attackType = def.attack;
        data.effect = def.effect;
        data.mainColor = def.main;
        data.accentColor = def.accent;
        data.catalogIndex = catalogIndex;

        // reset extras, then apply base stats and the weapon's own tweaks
        data.areaRadius = 0f; data.singleUse = false; data.affectsArea = false; data.returnsAfterThrow = false;
        data.spawnsBlock = false; data.glows = false; data.projectileGravity = false;
        data.projectileShape = PrimitiveType.Sphere; data.projectileSize = 0.12f; data.projectileSpeed = 20f;
        WeaponCatalog.ApplyBaseStats(data, def, indexInCategory);
        def.tweak?.Invoke(data);
        data.damage = Mathf.Round(data.damage);
        data.price = WeaponCatalog.PriceFor(data);

        data.shape = def.look(def.main, def.accent).parts;

        if (isNew) AssetDatabase.CreateAsset(data, path);
        else EditorUtility.SetDirty(data);
        return data;
    }

    static void BuildPrefab(WeaponData data)
    {
        var root = new GameObject(data.weaponName);
        try
        {
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = data.mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            root.AddComponent<Grabbable>();
            root.AddComponent<Weapon>().data = data;

            for (int i = 0; i < data.shape.Count; i++)
            {
                var part = data.shape[i];
                var go = GameObject.CreatePrimitive(part.shape);
                go.name = $"Part {i + 1} ({part.shape})";
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = part.offset;
                go.transform.localRotation = Quaternion.Euler(part.rotation);
                go.transform.localScale = part.size;
                go.AddComponent<PrimitiveColor>().color = part.color;

                // Cylinders come with a round capsule collider; a box fits flat discs (like the pan) much better.
                if (part.shape == PrimitiveType.Cylinder)
                {
                    Object.DestroyImmediate(go.GetComponent<Collider>());
                    go.AddComponent<BoxCollider>();
                }
            }

            // Measure the weapon so the generator knows where the tip is.
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
            data.length = Mathf.Max(0.1f, bounds.max.z);
            data.tipOffset = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z);
            EditorUtility.SetDirty(data);

            if (data.glows)
            {
                var lightGO = new GameObject("Glow");
                lightGO.transform.SetParent(root.transform, false);
                lightGO.transform.localPosition = data.tipOffset * 0.85f;
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = data.accentColor == Color.white ? data.mainColor : Color.Lerp(data.mainColor, data.accentColor, 0.5f);
                light.range = 3f;
                light.intensity = 2f;
                light.shadows = LightShadows.None;
            }

            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{FileName(data.weaponName)}.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    public static string FileName(string weaponName)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) weaponName = weaponName.Replace(c, '_');
        return weaponName;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
