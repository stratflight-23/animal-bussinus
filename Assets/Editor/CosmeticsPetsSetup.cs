using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menus:
//   Gorilla Company > 10. Generate Cosmetics  -> 30 cosmetics in Assets/Cosmetics/Data + the library + the "SelfHidden" layer
//   Gorilla Company > 11. Add Wardrobe        -> mirror + wardrobe panel (Treehouse Base, or next to the player)
//   Gorilla Company > 12. Generate Pets       -> 10 pets in Assets/Pets/Data + the library
//   Gorilla Company > 13. Add Pet House       -> pet house + panel next to the wardrobe
// Command line: -executeMethod CosmeticsPetsSetup.GenerateAllFromCommandLine
public static class CosmeticsPetsSetup
{
    const string CosmeticFolder = "Assets/Cosmetics/Data";
    const string PetFolder = "Assets/Pets/Data";
    const string LibraryFolder = "Assets/Resources/GorillaCompany";
    const string MirrorFolder = "Assets/Materials/Wardrobe";

    // where they go in the Jungle Temple map's Treehouse Base (south side of the deck, facing spawn)
    public static readonly Vector3 MapWardrobePos = new Vector3(-2.2f, 8f, -9.5f);
    public static readonly Vector3 MapPetHousePos = new Vector3(2.9f, 8f, -9.3f);

    // ================================================================ menus

    [MenuItem("Gorilla Company/10. Generate Cosmetics")]
    static void GenerateCosmeticsMenu()
    {
        int n = GenerateCosmetics();
        EditorUtility.DisplayDialog("Gorilla Company", $"Made {n} cosmetics (all FREE) in {CosmeticFolder}.\n\nNext: 11. Add Wardrobe.", "OK");
    }

    [MenuItem("Gorilla Company/11. Add Wardrobe")]
    static void AddWardrobeMenu()
    {
        if (CosmeticLibrary.Load() == null) GenerateCosmetics();
        if (Object.FindAnyObjectByType<Wardrobe>() != null) { Already("Wardrobe"); return; }
        Place(out Transform parent, out Vector3 pos, out float yaw, MapWardrobePos, 2.5f);
        var w = BuildWardrobe(parent, pos, yaw);
        Undo.RegisterCreatedObjectUndo(w, "Add Wardrobe");
        GorillaCombatSetup.Finish(w, "Wardrobe added! Save the scene (Ctrl+S) and press Play.\n\n" +
            "Poke the buttons to pick a slot, browse, and WEAR. Look in the mirror to see yourself.\n" +
            "No headset? Stand near it: C = next slot, Z / X = prev / next, V = wear, B = remove.");
    }

    [MenuItem("Gorilla Company/12. Generate Pets")]
    static void GeneratePetsMenu()
    {
        int n = GeneratePets();
        EditorUtility.DisplayDialog("Gorilla Company", $"Made {n} pets (all FREE) in {PetFolder}.\n\nNext: 13. Add Pet House.", "OK");
    }

    [MenuItem("Gorilla Company/13. Add Pet House")]
    static void AddPetHouseMenu()
    {
        if (PetLibrary.Load() == null) GeneratePets();
        if (Object.FindAnyObjectByType<PetHouse>() != null) { Already("Pet House"); return; }
        Place(out Transform parent, out Vector3 pos, out float yaw, MapPetHousePos, -2.5f);
        var h = BuildPetHouse(parent, pos, yaw);
        Undo.RegisterCreatedObjectUndo(h, "Add Pet House");
        GorillaCombatSetup.Finish(h, "Pet House added! Save the scene (Ctrl+S) and press Play.\n\n" +
            "Poke PREV / NEXT to pick a pet, then SUMMON. Touch your pet with your hand to pet it.\n" +
            "No headset? Stand near it: N / M = prev / next, P = summon, O = put away.");
    }

    public static void GenerateAllFromCommandLine()
    {
        Debug.Log($"[Cosmetics] {GenerateCosmetics()} cosmetics, [Pets] {GeneratePets()} pets generated.");
    }

    static void Already(string what)
    {
        EditorUtility.DisplayDialog("Gorilla Company", $"This scene already has a {what}. Delete it first if you want a fresh one.", "OK");
    }

    // In the Jungle Temple map: on the Treehouse deck. Otherwise: a couple of meters to the side of the player, facing them.
    static void Place(out Transform parent, out Vector3 pos, out float yaw, Vector3 mapPos, float sideOffset)
    {
        var baseArea = GameObject.Find("Area 1 - Treehouse Base");
        if (baseArea != null)
        {
            parent = baseArea.transform;
            pos = mapPos;
            yaw = 0f;
            return;
        }
        parent = null;
        var player = GameObject.Find("GorillaPlayer");
        Vector3 p = player != null ? player.transform.position : Vector3.zero;
        Vector3 fwd = player != null ? player.transform.forward : Vector3.forward;
        fwd.y = 0f;
        fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        pos = GorillaCombatSetup.GroundAt(p + fwd * 3f + right * sideOffset);
        Vector3 back = p - pos;
        back.y = 0f;
        yaw = Quaternion.LookRotation(back.sqrMagnitude > 0.01f ? back : -fwd).eulerAngles.y;
    }

    // ================================================================ generating data

    public static int GenerateCosmetics()
    {
        EnsureLayer(PlayerAvatar.SelfHiddenLayerName);
        EnsureFolder(CosmeticFolder);
        EnsureFolder(LibraryFolder);
        var lib = LoadOrCreate<CosmeticLibrary>(LibraryFolder + "/CosmeticLibrary.asset");
        lib.all.Clear();
        foreach (var d in CosmeticCatalog.All())
        {
            var c = LoadOrCreate<CosmeticData>($"{CosmeticFolder}/{FileName(d.name)}.asset");
            c.id = d.id;
            c.displayName = d.name;
            c.slot = d.slot;
            c.description = d.desc;
            c.price = 0;  // everything is FREE
            c.parts = d.parts;
            c.bodyStyle = d.style;
            c.bodyColor = d.bodyColor;
            EditorUtility.SetDirty(c);
            lib.all.Add(c);
        }
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        return lib.all.Count;
    }

    public static int GeneratePets()
    {
        EnsureFolder(PetFolder);
        EnsureFolder(LibraryFolder);
        var lib = LoadOrCreate<PetLibrary>(LibraryFolder + "/PetLibrary.asset");
        lib.all.Clear();
        foreach (var d in PetCatalog.All())
        {
            var p = LoadOrCreate<PetData>($"{PetFolder}/{FileName(d.name)}.asset");
            p.id = d.id;
            p.displayName = d.name;
            p.description = d.desc;
            p.price = 0;
            p.parts = d.parts;
            p.size = d.size;
            p.followSpeed = d.speed;
            p.flies = d.flies;
            p.bouncy = d.bouncy;
            p.idleTrick = d.trick;
            p.tailPivot = d.tailPivot;
            EditorUtility.SetDirty(p);
            lib.all.Add(p);
        }
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        return lib.all.Count;
    }

    // ================================================================ building the wardrobe + pet house
    // Both face their local +Z: stand on that side to use them.

    public static GameObject BuildWardrobe(Transform parent, Vector3 pos, float yaw)
    {
        var root = new GameObject("Wardrobe");
        if (parent != null) root.transform.SetParent(parent, true);
        root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        var t = root.transform;
        Color wood = new Color(0.5f, 0.32f, 0.16f), dark = new Color(0.3f, 0.18f, 0.09f), gold = new Color(1f, 0.78f, 0.2f);

        // big mirror in a wooden frame
        Box(t, "Mirror Frame", new Vector3(-0.45f, 1.2f, -0.02f), new Vector3(1.5f, 2.3f, 0.1f), wood);
        Box(t, "Mirror Top", new Vector3(-0.45f, 2.42f, -0.02f), new Vector3(1.7f, 0.15f, 0.16f), gold);
        Box(t, "Mirror Foot", new Vector3(-0.45f, 0.05f, 0.1f), new Vector3(1.6f, 0.1f, 0.4f), dark);
        var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glass.name = "Mirror Glass";
        Object.DestroyImmediate(glass.GetComponent<Collider>());
        glass.transform.SetParent(t, false);
        glass.transform.localPosition = new Vector3(-0.45f, 1.2f, 0.035f);
        glass.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // shows on the +Z side
        glass.transform.localScale = new Vector3(1.3f, 2.1f, 1f);
        var rt = MirrorTexture();
        glass.GetComponent<Renderer>().sharedMaterial = MirrorMaterial(rt);
        glass.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var camGo = new GameObject("Mirror Camera");
        camGo.transform.SetParent(t, false);
        camGo.transform.localPosition = new Vector3(-0.45f, 1.45f, 0.06f);
        var cam = camGo.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 15f;
        cam.enabled = false;              // MagicMirror turns it on when you're close
        root.AddComponent<MagicMirror>().mirrorCamera = cam;

        // wardrobe panel on a post + a stand that shows the selected item
        Box(t, "Panel Post", new Vector3(0.75f, 0.55f, 0f), new Vector3(0.12f, 1.1f, 0.12f), dark);
        var panel = new GameObject("Wardrobe Panel");
        panel.transform.SetParent(t, false);
        panel.transform.localPosition = new Vector3(0.75f, 1.35f, 0.05f);
        panel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // poke panels face their -Z, so turn it around
        var wardrobe = panel.AddComponent<Wardrobe>();
        wardrobe.library = AssetDatabase.LoadAssetAtPath<CosmeticLibrary>(LibraryFolder + "/CosmeticLibrary.asset");

        Cyl(t, "Display Stand", new Vector3(1.55f, 0.45f, 0.2f), 0.22f, 0.9f, wood);
        Cyl(t, "Display Top", new Vector3(1.55f, 0.92f, 0.2f), 0.26f, 0.05f, gold);
        var spot = new GameObject("Preview Spot").transform;
        spot.SetParent(t, false);
        spot.localPosition = new Vector3(1.55f, 1.15f, 0.2f);
        wardrobe.previewSpot = spot;

        Sign(t, "WARDROBE\n<size=55%>every item is FREE!</size>", new Vector3(0.6f, 2.75f, 0f), new Vector2(2.6f, 0.6f));
        return root;
    }

    public static GameObject BuildPetHouse(Transform parent, Vector3 pos, float yaw)
    {
        var root = new GameObject("Pet House");
        if (parent != null) root.transform.SetParent(parent, true);
        root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        var t = root.transform;
        Color wood = new Color(0.55f, 0.36f, 0.18f), roof = new Color(0.75f, 0.25f, 0.2f), dark = new Color(0.15f, 0.1f, 0.06f);

        // little kennel with a red roof and a dark doorway
        Box(t, "Kennel", new Vector3(0f, 0.55f, 0f), new Vector3(1.4f, 1.1f, 1.2f), wood);
        var r1 = Box(t, "Roof", new Vector3(-0.38f, 1.32f, 0f), new Vector3(0.95f, 0.08f, 1.35f), roof);
        r1.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        var r2 = Box(t, "Roof", new Vector3(0.38f, 1.32f, 0f), new Vector3(0.95f, 0.08f, 1.35f), roof);
        r2.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        NoCollider(Box(t, "Doorway", new Vector3(0f, 0.4f, 0.601f), new Vector3(0.55f, 0.65f, 0.01f), dark));
        NoCollider(Box(t, "Bone Sign", new Vector3(0f, 0.92f, 0.605f), new Vector3(0.5f, 0.1f, 0.01f), Color.white));

        // preview stand in front, and the panel on a post to the side
        Cyl(t, "Preview Stand", new Vector3(0f, 0.08f, 1.15f), 0.4f, 0.16f, new Color(0.3f, 0.6f, 0.3f));
        var spot = new GameObject("Preview Spot").transform;
        spot.SetParent(t, false);
        spot.localPosition = new Vector3(0f, 0.17f, 1.15f);

        Box(t, "Panel Post", new Vector3(-1.45f, 0.55f, 0.3f), new Vector3(0.12f, 1.1f, 0.12f), dark);
        var panel = new GameObject("Pet House Panel");
        panel.transform.SetParent(t, false);
        panel.transform.localPosition = new Vector3(-1.45f, 1.3f, 0.35f);
        panel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var house = panel.AddComponent<PetHouse>();
        house.library = AssetDatabase.LoadAssetAtPath<PetLibrary>(LibraryFolder + "/PetLibrary.asset");
        house.previewSpot = spot;

        Sign(t, "PET HOUSE\n<size=55%>every pet is FREE!</size>", new Vector3(0f, 1.95f, 0.2f), new Vector2(2f, 0.55f));
        return root;
    }

    // ================================================================ mirror assets

    static RenderTexture MirrorTexture()
    {
        EnsureFolder(MirrorFolder);
        string path = MirrorFolder + "/MirrorView.renderTexture";
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (rt != null) return rt;
        rt = new RenderTexture(512, 820, 24) { name = "MirrorView", antiAliasing = 1 };   // small = fast on Quest
        AssetDatabase.CreateAsset(rt, path);
        return rt;
    }

    static Material MirrorMaterial(RenderTexture rt)
    {
        string path = MirrorFolder + "/MirrorGlass.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(shader) { name = "MirrorGlass" };
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", rt);
        mat.mainTexture = rt;
        mat.SetColor("_BaseColor", Color.white);
        mat.mainTextureScale = new Vector2(-1f, 1f);    // flip left/right, like a real mirror
        mat.mainTextureOffset = new Vector2(1f, 0f);
        mat.SetTextureScale("_BaseMap", new Vector2(-1f, 1f));
        mat.SetTextureOffset("_BaseMap", new Vector2(1f, 0f));
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ================================================================ helpers

    static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.AddComponent<PrimitiveColor>().color = color;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static GameObject Cyl(Transform parent, string name, Vector3 localPos, float radius, float height, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
        go.AddComponent<PrimitiveColor>().color = color;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static GameObject NoCollider(GameObject go)
    {
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void Sign(Transform parent, string text, Vector3 localPos, Vector2 box)
    {
        var go = new GameObject("Sign");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 2.4f;
        tmp.color = new Color(1f, 0.92f, 0.6f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = box;
        go.AddComponent<FaceCamera>();
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;
        a = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(a, path);
        return a;
    }

    static string FileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // Adds a layer (like "SelfHidden") to Project Settings > Tags and Layers if it isn't there yet.
    static void EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;
        for (int i = 8; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[Cosmetics] Added layer '{layerName}' (layer {i}).");
                return;
            }
        }
        Debug.LogWarning("[Cosmetics] No free layer for " + layerName + ": your own hats may block your view.");
    }
}
