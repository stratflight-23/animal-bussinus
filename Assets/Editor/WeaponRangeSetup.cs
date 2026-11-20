using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Gorilla Company > 5. Add Weapon Range
// Adds 10 racks (one per category) holding all 100 weapons with name labels, plus 5 training dummies,
// and lets the Mod Panel's SPAWN / NEXT buttons spawn any weapon.
public static class WeaponRangeSetup
{
    const float SlotSpacing = 0.5f;
    const float RackGap = 1.0f;
    const float WeaponHeight = 0.95f; // where the handle sits above the ground

    [MenuItem("Gorilla Company/5. Add Weapon Range")]
    static void AddRange()
    {
        var weapons = LoadWeapons();
        if (weapons.Count == 0)
        {
            EditorUtility.DisplayDialog("Gorilla Company", "No weapons found yet.\n\nRun Gorilla Company > 4. Generate All Weapons first.", "OK");
            return;
        }

        var existing = GameObject.Find("Weapon Range");
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorUtility.DisplayDialog("Gorilla Company",
                "This scene already has a Weapon Range (now selected).\n\nIf you want a fresh one, delete it first (select it and press Delete), then run this again.", "OK");
            return;
        }

        var player = GorillaCombatSetup.EnsurePlayer();
        Vector3 origin = player != null ? new Vector3(player.transform.position.x, 0f, player.transform.position.z) : Vector3.zero;

        var root = new GameObject("Weapon Range");
        Undo.RegisterCreatedObjectUndo(root, "Add Weapon Range");
        root.transform.position = origin;

        // ---- racks: 2 rows of 5, behind the player's start (toward -Z), facing the player
        var categories = weapons.GroupBy(w => w.data.category).ToList();
        float rackWidth = 10 * SlotSpacing + 0.2f;
        for (int c = 0; c < categories.Count; c++)
        {
            int row = c / 5, col = c % 5;
            float x = (col - 2) * (rackWidth + RackGap);
            float z = -12f - row * 5f;
            Vector3 pos = GorillaCombatSetup.GroundAt(origin + new Vector3(x, 0f, z));
            BuildRack(root.transform, categories[c].Key, categories[c].ToList(), pos);
        }

        // ---- 5 training dummies between the player and the racks
        var dummies = new GameObject("Training Dummies").transform;
        dummies.SetParent(root.transform, false);
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = GorillaCombatSetup.GroundAt(origin + new Vector3((i - 2) * 2.2f, 0f, -7.5f));
            var dummy = TrainingDummy.Create(pos, "Training Dummy " + (i + 1));
            dummy.transform.SetParent(dummies, true);
            dummy.transform.rotation = Quaternion.LookRotation(Vector3.forward); // facing the player start
        }

        // ---- Mod Panel spawn list
        string modNote = FillModSpawnables(player, weapons.Select(w => w.prefab).ToArray());

        GorillaCombatSetup.Finish(root,
            $"Weapon Range added with {weapons.Count} weapons on {categories.Count} racks and 5 training dummies.\n\n" +
            "The racks are behind where the player starts (turn around!).\n" + modNote +
            "\n\nSave the scene with Ctrl+S.");
    }

    struct Entry { public WeaponData data; public GameObject prefab; }

    static List<Entry> LoadWeapons()
    {
        var list = new List<Entry>();
        foreach (var guid in AssetDatabase.FindAssets("t:WeaponData", new[] { WeaponGenerator.DataFolder }))
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{WeaponGenerator.PrefabFolder}/{WeaponGenerator.FileName(data.weaponName)}.prefab");
            if (prefab != null) list.Add(new Entry { data = data, prefab = prefab });
        }
        return list.OrderBy(e => e.data.catalogIndex).ToList();
    }

    static void BuildRack(Transform parent, string category, List<Entry> items, Vector3 position)
    {
        var rack = new GameObject("Rack - " + category).transform;
        rack.SetParent(parent, false);
        rack.position = position;
        rack.rotation = Quaternion.identity; // weapons face +Z, toward the player start

        float width = items.Count * SlotSpacing + 0.2f;
        Color wood = new Color(0.55f, 0.36f, 0.18f);
        Color dark = new Color(0.36f, 0.22f, 0.1f);

        Shape(rack, PrimitiveType.Cube, "Table", new Vector3(0f, 0.4f, 0f), new Vector3(width, 0.8f, 0.6f), wood, true);
        Shape(rack, PrimitiveType.Cube, "Back Board", new Vector3(0f, 1.6f, -0.34f), new Vector3(width, 2.4f, 0.08f), dark, true);
        Shape(rack, PrimitiveType.Cube, "Sign Board", new Vector3(0f, 3.05f, -0.3f), new Vector3(width * 0.7f, 0.45f, 0.06f), new Color(0.95f, 0.8f, 0.3f), true);
        Label(rack, category.ToUpper(), new Vector3(0f, 3.05f, -0.26f), 2.4f, new Vector2(width * 0.7f, 0.45f), Color.black, false);

        for (int i = 0; i < items.Count; i++)
        {
            var e = items[i];
            float x = (i - (items.Count - 1) / 2f) * SlotSpacing;

            var slot = new GameObject("Slot - " + e.data.weaponName);
            slot.transform.SetParent(rack, false);
            slot.transform.localPosition = new Vector3(x, WeaponHeight, 0f);
            slot.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // stand the weapon up (tip pointing up)

            Shape(rack, PrimitiveType.Cylinder, "Peg", new Vector3(x, 0.85f, 0f), new Vector3(0.08f, 0.05f, 0.08f), dark, false);

            var weapon = (GameObject)PrefabUtility.InstantiatePrefab(e.prefab, slot.transform);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.GetComponent<Rigidbody>().isKinematic = true; // stays on the rack until grabbed

            var rackSlot = slot.AddComponent<WeaponRackSlot>();
            rackSlot.prefab = e.prefab;
            rackSlot.current = weapon.GetComponent<Grabbable>();

            // lift long weapons (spears...) so they don't poke through the table
            Bounds b = WorldBounds(weapon);
            float tableTop = rack.position.y + 0.82f;
            if (b.min.y < tableTop)
            {
                slot.transform.position += Vector3.up * (tableTop - b.min.y);
                b = WorldBounds(weapon);
            }

            float labelY = b.max.y - rack.position.y + 0.18f;
            string text = $"<b>{e.data.weaponName}</b>\n<size=70%>{e.data.attackType} · {e.data.price} coins</size>";
            Label(rack, text, new Vector3(x, labelY, 0f), 0.55f, new Vector2(SlotSpacing - 0.02f, 0.3f), Color.white, true);
        }
    }

    static Bounds WorldBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }

    static void Shape(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Color color, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.AddComponent<PrimitiveColor>().color = color;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    static void Label(Transform parent, string text, Vector3 localPos, float size, Vector2 box, Color color, bool faceCamera)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // readable from the player's side
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.rectTransform.sizeDelta = box;
        if (faceCamera) go.AddComponent<FaceCamera>();
    }

    static string FillModSpawnables(GameObject player, GameObject[] prefabs)
    {
        var mod = Object.FindAnyObjectByType<ModPowers>();
        if (mod == null && player != null) mod = Undo.AddComponent<ModPowers>(player);
        if (mod == null) return "(No GorillaPlayer found, so the Mod Panel spawn list was not filled.)";

        Undo.RecordObject(mod, "Fill Mod Spawnables");
        mod.spawnables = prefabs;
        EditorUtility.SetDirty(mod);
        return $"The Mod Panel's SPAWN / NEXT buttons can now spawn all {prefabs.Length} weapons.";
    }
}
