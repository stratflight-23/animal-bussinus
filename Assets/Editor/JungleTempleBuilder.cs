using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Gorilla Company > 9. Build Jungle Temple Map
// Builds our original map from simple shapes and saves it as Assets/Scenes/JungleTemple.unity,
// then makes it the first scene in Build Settings (other scenes, like GorillaTest, stay in the list but turned off).
//
//   AREA 1 - TREEHOUSE BASE (around 0,0,0)  : spawn + safe zone, Mod Panel, Sell Bin, Shop Stand, weapon rack spot,
//                                             coin sign, rope bridges, Temple Portal. Bright daytime.
//   AREA 2 - JUNGLE TEMPLE  (around 0,0,400): 60 x 60 m stone dungeon, 2 floors, 10 rooms, lava, spikes, moving
//                                             platforms, 25 loot spots, 8 monster markers. Dark, lit by torches + crystals.
//
// Running it again rebuilds the whole scene from scratch (hand edits to JungleTemple.unity would be lost).
public static class JungleTempleBuilder
{
    const string ScenePath = "Assets/Scenes/JungleTemple.unity";
    const string MatFolder = "Assets/Materials/JungleTemple";
    const string GenFolder = "Assets/Scenes/JungleTemple_Generated";
    const string KittyPrefabPath = "Assets/Weapons/Mod/Kitty Blaster.prefab";
    const string ElevatorPrefabPath = "Assets/Prefabs/Moving_Platform.prefab";

    static readonly Vector3 TempleOrigin = new Vector3(0f, 0f, 400f);
    const float DeckY = 8f;      // top of the treehouse deck
    const float F2 = 5f;         // temple upstairs floor height
    const float Top = 10f;       // temple roof (underside)
    const float WallT = 0.6f;    // wall thickness

    static Material wood, darkWood, bark, leaves, rope, stone, darkStone, moss, metal;
    static Material portalGlow, lavaGlow, torchGlow, crystalCyan, crystalPurple;
    static Material grass, runes, gold, stripes;

    // ================================================================ menu

    [MenuItem("Gorilla Company/9. Build Jungle Temple Map")]
    static void BuildMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Gorilla Company",
                "JungleTemple.unity already exists. Rebuild it from scratch?\n(Hand edits in that scene will be lost.)",
                "Rebuild", "Cancel"))
            return;

        var player = Build();
        Selection.activeGameObject = player;
        EditorUtility.DisplayDialog("Gorilla Company",
            "Jungle Temple map built and saved as " + ScenePath + ".\nIt's now the first scene in Build Settings.\n\n" +
            "Press Play to walk around. Walk into the big green Temple Portal to go to the temple.\n" +
            "Bring loot back and drop it in the Sell Bin for Goop Coins!", "OK");
    }

    // For the command line: -executeMethod JungleTempleBuilder.BuildFromCommandLine
    public static void BuildFromCommandLine()
    {
        Build();
        Debug.Log("[Jungle Temple] Built " + ScenePath);
    }

    static GameObject Build()
    {
        LoadMaterials();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null) Object.DestroyImmediate(defaultCam);

        var sun = Object.FindAnyObjectByType<Light>();
        if (sun != null)
        {
            sun.name = "Sun";
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.None;   // no real-time shadows: much faster on Quest
        }

        var map = new GameObject("Jungle Temple Map").transform;
        var baseRoot = Group(map, "Area 1 - Treehouse Base", Vector3.zero);
        var templeRoot = Group(map, "Area 2 - Jungle Temple", TempleOrigin);

        BuildBase(baseRoot, out Transform spawnPoint, out Transform basePortal, out Transform baseArrival);
        BuildTemple(templeRoot, out Transform templePortal, out Transform templeArrival);

        // link the portals
        var toTemple = basePortal.GetComponent<TemplePortal>();
        toTemple.destination = templeArrival;
        toTemple.respawnTempleLoot = true;
        toTemple.templeArea = TempleBounds();
        templePortal.GetComponent<TemplePortal>().destination = baseArrival;

        // ---- player
        var player = GorillaCompanySetup.CreatePlayer(spawnPoint.position, spawnPoint.rotation);
        var cam = player.GetComponentInChildren<Camera>();
        if (cam != null) cam.farClipPlane = 250f; // the temple (400 m away) is never drawn from the base
        GorillaCombatSetup.EnsurePlayer();
        if (player.GetComponent<PlayerHealthHud>() == null) player.AddComponent<PlayerHealthHud>();
        var mod = player.GetComponent<ModPowers>();
        if (mod == null) mod = player.AddComponent<ModPowers>();
        mod.kittyBlasterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KittyPrefabPath);
        foreach (var t in player.GetComponentsInChildren<Transform>(true))
            if ((t.name == "Left Hand" || t.name == "Right Hand") && t.GetComponent<HandPoker>() == null)
                t.gameObject.AddComponent<HandPoker>();
        player.GetComponent<PlayerDeathPhysics>().respawnPoint = spawnPoint;

        // ---- Mod Panel (off to the side of spawn, facing it)
        var panel = new GameObject("Mod Panel");
        panel.transform.SetParent(baseRoot, false);
        Vector3 panelPos = new Vector3(-5.5f, DeckY + 1.2f, -8.3f);
        Vector3 away = panelPos - spawnPoint.position;
        away.y = 0f;
        panel.transform.SetPositionAndRotation(panelPos, Quaternion.LookRotation(away));
        panel.AddComponent<ModKeypad>();
        Box(baseRoot, "Mod Panel Stand", new Vector3(-5.5f, DeckY + 0.42f, -8.25f), new Vector3(0.15f, 0.85f, 0.15f), darkWood);

        // ---- Spawn safe zone
        var safe = new GameObject("Spawn Safe Zone");
        safe.transform.SetParent(baseRoot, false);
        safe.transform.position = new Vector3(spawnPoint.position.x, DeckY, spawnPoint.position.z);
        safe.AddComponent<SpawnSafeZone>().radius = 3.05f;

        // ---- lighting per area (bright base, dark temple)
        var lighting = new GameObject("Area Lighting").AddComponent<MapAreaLighting>();
        lighting.transform.SetParent(map, false);
        lighting.sun = sun;
        var templeBounds = TempleBounds();
        lighting.areas = new[]
        {
            new MapAreaLighting.AreaLook
            {
                name = "Treehouse Base", center = new Vector3(0f, 20f, 0f), size = new Vector3(240f, 120f, 240f),
                ambient = new Color(0.55f, 0.6f, 0.55f), sunOn = true, sunIntensity = 1.2f,
                fogColor = new Color(0.62f, 0.75f, 0.66f), fogStart = 45f, fogEnd = 170f, reflections = 1f,
            },
            new MapAreaLighting.AreaLook
            {
                name = "Jungle Temple", center = templeBounds.center, size = templeBounds.size,
                ambient = new Color(0.07f, 0.07f, 0.09f), sunOn = false, sunIntensity = 0f,
                fogColor = new Color(0.02f, 0.025f, 0.03f), fogStart = 10f, fogEnd = 55f, reflections = 0.1f,
            },
        };
        // editor preview uses the base look
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = lighting.areas[0].ambient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = lighting.areas[0].fogColor;
        RenderSettings.fogStartDistance = lighting.areas[0].fogStart;
        RenderSettings.fogEndDistance = lighting.areas[0].fogEnd;

        // ---- save + build settings
        EditorSceneManager.SaveScene(scene, ScenePath);
        var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != ScenePath)
                buildScenes.Add(new EditorBuildSettingsScene(s.path, false)); // keep GorillaTest etc. in the list, turned off
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        return player;
    }

    static Bounds TempleBounds() => new Bounds(TempleOrigin + new Vector3(0f, 10f, -5f), new Vector3(170f, 80f, 170f));

    // ================================================================ AREA 1: TREEHOUSE BASE

    static void BuildBase(Transform root, out Transform spawnPoint, out Transform portal, out Transform arrival)
    {
        float y = DeckY;

        // jungle floor far below
        Box(root, "Jungle Floor", new Vector3(0f, -0.5f, 0f), new Vector3(140f, 1f, 140f), grass);

        // the big deck (20 x 20 m) with plank lines and support beams
        var deck = Group(root, "Treehouse Deck", Vector3.zero);
        Box(deck, "Deck", new Vector3(0f, y - 0.2f, 0f), new Vector3(20f, 0.4f, 20f), wood);
        for (int i = -4; i <= 4; i++)
            NoCollider(Box(deck, "Plank Line", new Vector3(0f, y + 0.003f, i * 2.2f), new Vector3(19.8f, 0.01f, 0.06f), darkWood));
        Box(deck, "Edge Beam", new Vector3(0f, y - 0.55f, -10f), new Vector3(20.4f, 0.4f, 0.4f), darkWood);
        Box(deck, "Edge Beam", new Vector3(0f, y - 0.55f, 10f), new Vector3(20.4f, 0.4f, 0.4f), darkWood);
        Box(deck, "Edge Beam", new Vector3(-10f, y - 0.55f, 0f), new Vector3(0.4f, 0.4f, 20.4f), darkWood);
        Box(deck, "Edge Beam", new Vector3(10f, y - 0.55f, 0f), new Vector3(0.4f, 0.4f, 20.4f), darkWood);

        // four giant trees growing up through the deck corners (climbable!)
        var trees = Group(root, "Giant Trees", Vector3.zero);
        foreach (var c in new[] { new Vector2(-8.5f, -8.5f), new Vector2(8.5f, -8.5f), new Vector2(-8.5f, 8.5f), new Vector2(8.5f, 8.5f) })
            GiantTree(trees, new Vector3(c.x, 0f, c.y), 1.4f, 30f);

        // railings with gaps for the bridges (east/west) and the portal (north)
        var rails = Group(root, "Railings", Vector3.zero);
        Railing(rails, new Vector3(-10f, y, -10f), new Vector3(10f, y, -10f));                                   // south
        Railing(rails, new Vector3(-10f, y, 10f), new Vector3(-2.6f, y, 10f));                                   // north (left of portal)
        Railing(rails, new Vector3(2.6f, y, 10f), new Vector3(10f, y, 10f));                                     // north (right of portal)
        Railing(rails, new Vector3(10f, y, -10f), new Vector3(10f, y, -1f));                                     // east
        Railing(rails, new Vector3(10f, y, 1f), new Vector3(10f, y, 10f));
        Railing(rails, new Vector3(-10f, y, -10f), new Vector3(-10f, y, -1f));                                   // west
        Railing(rails, new Vector3(-10f, y, 1f), new Vector3(-10f, y, 10f));

        // two lookout trees joined by rope bridges
        var east = Group(root, "East Lookout", Vector3.zero);
        GiantTree(east, new Vector3(24f, 0f, 0f), 1.2f, 26f);
        Lookout(east, new Vector3(24f, y, 0f));
        RopeBridge(root, new Vector3(10f, y, 0f), new Vector3(21f, y, 0f));
        var west = Group(root, "West Lookout", Vector3.zero);
        GiantTree(west, new Vector3(-24f, 0f, 0f), 1.2f, 26f);
        Lookout(west, new Vector3(-24f, y, 0f));
        RopeBridge(root, new Vector3(-10f, y, 0f), new Vector3(-21f, y, 0f));

        // a few background trees
        var bg = Group(root, "Background Trees", Vector3.zero);
        foreach (var p in new[] { new Vector2(-30, 25), new Vector2(32, 22), new Vector2(-34, -24), new Vector2(28, -30),
                                  new Vector2(0, -36), new Vector2(0, 36), new Vector2(-45, 2), new Vector2(46, -6) })
            GiantTree(bg, new Vector3(p.x, 0f, p.y), 1.1f, 22f);

        // spawn point (facing north, toward the portal)
        spawnPoint = new GameObject("Spawn Point").transform;
        spawnPoint.SetParent(root, false);
        spawnPoint.position = new Vector3(0f, y + 0.05f, -4f);

        SellBin(root, new Vector3(6.2f, y, -6.2f));
        ShopStand(root, new Vector3(-6.5f, y, 3.5f));
        WeaponRackSpot(root, new Vector3(7f, y, 3.5f));

        // coin scoreboard (near the portal, facing spawn)
        var board = Group(root, "Coin Scoreboard", new Vector3(-5.5f, y, 9.2f));
        LBox(board, "Post", new Vector3(-1.3f, 1.2f, 0f), new Vector3(0.15f, 2.4f, 0.15f), darkWood);
        LBox(board, "Post", new Vector3(1.3f, 1.2f, 0f), new Vector3(0.15f, 2.4f, 0.15f), darkWood);
        LBox(board, "Board", new Vector3(0f, 2.1f, 0f), new Vector3(3f, 1.4f, 0.12f), wood);
        var coins = LLabel(board, "GOOP COINS\n0", new Vector3(0f, 2.1f, -0.08f), 2.6f, new Vector2(2.8f, 1.3f), new Color(1f, 0.85f, 0.25f), 180f);
        board.gameObject.AddComponent<CoinScoreboard>().text = coins;

        // Temple Portal on the north edge
        portal = Portal(root, "Temple Portal", new Vector3(0f, y, 9.6f), 0f, "TO THE\nJUNGLE TEMPLE");

        arrival = new GameObject("Arrival From Temple").transform;
        arrival.SetParent(root, false);
        arrival.SetPositionAndRotation(new Vector3(0f, y + 0.05f, 6f), Quaternion.Euler(0f, 180f, 0f));
    }

    static void GiantTree(Transform parent, Vector3 basePos, float radius, float height)
    {
        var tree = Group(parent, "Giant Tree", basePos);
        LCyl(tree, "Trunk", new Vector3(0f, height / 2f, 0f), radius, height, bark);
        for (int i = 0; i < 4; i++)  // roots
        {
            float a = i * 90f + 45f;
            var root = LCyl(tree, "Root", Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.5f, radius * 1.1f), radius * 0.35f, 2.6f, bark,
                           new Vector3(55f, a, 0f));
            NoCollider(root);
        }
        // leafy canopy (no colliders, so you don't get stuck in leaves)
        NoCollider(LBall(tree, "Leaves", new Vector3(0f, height + 1f, 0f), new Vector3(radius * 9f, radius * 4.5f, radius * 9f), leaves));
        NoCollider(LBall(tree, "Leaves", new Vector3(radius * 3f, height - 1.5f, radius * 1.5f), new Vector3(radius * 6f, radius * 3.5f, radius * 6f), leaves));
        NoCollider(LBall(tree, "Leaves", new Vector3(-radius * 2.5f, height - 2.5f, -radius * 2f), new Vector3(radius * 6f, radius * 3f, radius * 6f), leaves));
        NoCollider(LCyl(tree, "Branch", new Vector3(radius * 1.8f, height - 3f, 0f), radius * 0.3f, radius * 4f, bark, new Vector3(0f, 0f, -60f)));
    }

    static void Lookout(Transform parent, Vector3 center)
    {
        Box(parent, "Lookout Deck", center + new Vector3(0f, -0.2f, 0f), new Vector3(6f, 0.4f, 6f), wood);
        Railing(parent, center + new Vector3(-3f, 0f, -3f), center + new Vector3(3f, 0f, -3f));
        Railing(parent, center + new Vector3(-3f, 0f, 3f), center + new Vector3(3f, 0f, 3f));
        float side = center.x > 0 ? 3f : -3f;     // the side away from the base
        Railing(parent, center + new Vector3(side, 0f, -3f), center + new Vector3(side, 0f, 3f));
        Railing(parent, center + new Vector3(-side, 0f, -3f), center + new Vector3(-side, 0f, -0.9f));
        Railing(parent, center + new Vector3(-side, 0f, 0.9f), center + new Vector3(-side, 0f, 3f));
    }

    // Wooden planks with small gaps, a little sag in the middle, and rope rails on both sides.
    static void RopeBridge(Transform parent, Vector3 from, Vector3 to)
    {
        var bridge = Group(parent, "Rope Bridge", Vector3.zero);
        Vector3 dir = to - from;
        float len = dir.magnitude;
        Vector3 fwd = dir / len;
        Vector3 side = Vector3.Cross(Vector3.up, fwd).normalized;
        Quaternion rot = Quaternion.LookRotation(fwd);
        int planks = Mathf.CeilToInt(len / 0.45f);
        for (int i = 0; i < planks; i++)
        {
            float t = (i + 0.5f) / planks;
            float sag = Mathf.Sin(Mathf.PI * t) * 0.35f;
            Vector3 p = Vector3.Lerp(from, to, t) + Vector3.down * (0.06f + sag);
            var plank = Box(bridge, "Plank", p, new Vector3(1.6f, 0.08f, 0.32f), i % 3 == 0 ? darkWood : wood);
            plank.transform.rotation = rot;
        }
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 off = side * 0.85f * s;
            var railRope = Cyl(bridge, "Rope Rail", (from + to) / 2f + off + Vector3.up * 0.95f, 0.04f, len, rope);
            railRope.transform.rotation = Quaternion.FromToRotation(Vector3.up, fwd);
            for (int i = 0; i <= Mathf.FloorToInt(len / 2f); i++)
            {
                float t = Mathf.Clamp01(i * 2f / len);
                float sag = Mathf.Sin(Mathf.PI * t) * 0.35f;
                NoCollider(Cyl(bridge, "Rope", Vector3.Lerp(from, to, t) + off + Vector3.up * (0.45f - sag / 2f), 0.03f, 1f + sag, rope));
            }
        }
        // invisible side walls so nobody slips off the bridge
        for (int s = -1; s <= 1; s += 2)
        {
            var guard = new GameObject("Bridge Guard");
            guard.transform.SetParent(bridge, false);
            guard.transform.SetPositionAndRotation((from + to) / 2f + side * 0.9f * s + Vector3.up * 0.5f, rot);
            guard.AddComponent<BoxCollider>().size = new Vector3(0.1f, 1.1f, len);
            MakeStatic(guard);
        }
    }

    static void Railing(Transform parent, Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.1f) return;
        Quaternion rot = Quaternion.LookRotation(d / len);
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2.5f));
        for (int i = 0; i <= posts; i++)
            Box(parent, "Rail Post", Vector3.Lerp(a, b, (float)i / posts) + Vector3.up * 0.55f, new Vector3(0.15f, 1.1f, 0.15f), darkWood);
        var top = Box(parent, "Top Rail", (a + b) / 2f + Vector3.up * 1.05f, new Vector3(0.12f, 0.1f, len), wood);
        top.transform.rotation = rot;
        var mid = Box(parent, "Mid Rail", (a + b) / 2f + Vector3.up * 0.55f, new Vector3(0.08f, 0.08f, len), wood);
        mid.transform.rotation = rot;
    }

    static void SellBin(Transform parent, Vector3 pos)
    {
        var bin = Group(parent, "Sell Bin", pos);
        LBox(bin, "Bottom", new Vector3(0f, 0.05f, 0f), new Vector3(1.6f, 0.1f, 1.6f), darkWood);
        LBox(bin, "Side", new Vector3(0f, 0.6f, -0.75f), new Vector3(1.6f, 1.1f, 0.12f), wood);
        LBox(bin, "Side", new Vector3(0f, 0.6f, 0.75f), new Vector3(1.6f, 1.1f, 0.12f), wood);
        LBox(bin, "Side", new Vector3(-0.75f, 0.6f, 0f), new Vector3(0.12f, 1.1f, 1.4f), wood);
        LBox(bin, "Side", new Vector3(0.75f, 0.6f, 0f), new Vector3(0.12f, 1.1f, 1.4f), wood);
        foreach (var x in new[] { -0.3f, 0.3f })    // crate slats
        {
            NoCollider(LBox(bin, "Slat", new Vector3(x, 0.6f, -0.82f), new Vector3(0.12f, 1.1f, 0.03f), darkWood));
            NoCollider(LBox(bin, "Slat", new Vector3(x, 0.6f, 0.82f), new Vector3(0.12f, 1.1f, 0.03f), darkWood));
        }
        // the glowing slot: a green frame on top + glowing goop inside
        NoCollider(LBox(bin, "Slot Glow", new Vector3(0f, 1.17f, -0.62f), new Vector3(1.3f, 0.06f, 0.08f), portalGlow));
        NoCollider(LBox(bin, "Slot Glow", new Vector3(0f, 1.17f, 0.62f), new Vector3(1.3f, 0.06f, 0.08f), portalGlow));
        NoCollider(LBox(bin, "Slot Glow", new Vector3(-0.62f, 1.17f, 0f), new Vector3(0.08f, 0.06f, 1.3f), portalGlow));
        NoCollider(LBox(bin, "Slot Glow", new Vector3(0.62f, 1.17f, 0f), new Vector3(0.08f, 0.06f, 1.3f), portalGlow));
        NoCollider(LBox(bin, "Goop Inside", new Vector3(0f, 0.25f, 0f), new Vector3(1.35f, 0.3f, 1.35f), portalGlow));

        var slot = new GameObject("Sell Slot (trigger)");
        slot.transform.SetParent(bin, false);
        slot.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        var trig = slot.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = new Vector3(1.35f, 1.2f, 1.35f);
        var popup = new GameObject("Popup Point").transform;
        popup.SetParent(bin, false);
        popup.localPosition = new Vector3(0f, 2.1f, 0f);
        slot.AddComponent<SellBin>().popupPoint = popup;

        // sign with the prices
        LBox(bin, "Sign Post", new Vector3(1.05f, 1.1f, 0.6f), new Vector3(0.12f, 2.2f, 0.12f), darkWood);
        LBox(bin, "Sign Board", new Vector3(1.05f, 2.05f, 0.6f), new Vector3(1.8f, 0.9f, 0.08f), wood, new Vector3(0f, -70f, 0f));
        string prices = "<b>SELL BIN</b>\n<size=55%>Drop loot in for Goop Coins!\n" +
                        "Goop Jar 20 · Coin Pile 30 · Toy Robot 40\nGolden Banana 50 · Shiny Gem 75\nCrystal Skull 120 · Temple Idol 200</size>";
        LLabel(bin, prices, new Vector3(1.05f, 2.05f, 0.6f) + Quaternion.Euler(0f, -70f, 0f) * new Vector3(0f, 0f, 0.06f),
              1.5f, new Vector2(1.7f, 0.85f), new Color(1f, 0.92f, 0.6f), -70f);
    }

    static void ShopStand(Transform parent, Vector3 pos)
    {
        var shop = Group(parent, "Shop Stand (coming soon)", pos);
        shop.rotation = Quaternion.Euler(0f, 90f, 0f); // counter faces the middle of the deck
        LBox(shop, "Counter", new Vector3(0f, 0.5f, 0f), new Vector3(3f, 1f, 0.8f), wood);
        LBox(shop, "Counter Top", new Vector3(0f, 1.03f, 0f), new Vector3(3.2f, 0.06f, 1f), darkWood);
        LBox(shop, "Post", new Vector3(-1.45f, 1.5f, 0.6f), new Vector3(0.15f, 3f, 0.15f), darkWood);
        LBox(shop, "Post", new Vector3(1.45f, 1.5f, 0.6f), new Vector3(0.15f, 3f, 0.15f), darkWood);
        var awning = LBox(shop, "Striped Awning", new Vector3(0f, 3.05f, 0.15f), new Vector3(3.4f, 0.08f, 1.6f), stripes);
        awning.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        LBox(shop, "Sign Board", new Vector3(0f, 2.4f, 0.65f), new Vector3(2.4f, 0.6f, 0.06f), wood);
        LLabel(shop, "SHOP\n<size=50%>coming soon!</size>", new Vector3(0f, 2.4f, 0.69f), 2.2f, new Vector2(2.3f, 0.6f),
              new Color(1f, 0.92f, 0.6f), 0f);
        var spot = new GameObject("Shop Items Spot (empty)");
        spot.transform.SetParent(shop, false);
        spot.transform.localPosition = new Vector3(0f, 1.1f, 0f);
    }

    static void WeaponRackSpot(Transform parent, Vector3 pos)
    {
        var rack = Group(parent, "Weapon Rack Spot (placeholder)", pos);
        rack.rotation = Quaternion.Euler(0f, -90f, 0f);   // local +Z = toward the middle of the deck
        LBox(rack, "Back Board", new Vector3(0f, 1.2f, -0.3f), new Vector3(3f, 2.4f, 0.1f), darkWood);
        LBox(rack, "Shelf", new Vector3(0f, 0.8f, 0f), new Vector3(3f, 0.1f, 0.6f), wood);
        for (int i = -2; i <= 2; i++)
            NoCollider(LCyl(rack, "Peg", new Vector3(i * 0.6f, 1.6f, -0.1f), 0.03f, 0.3f, wood, new Vector3(90f, 0f, 0f)));
        LLabel(rack, "WEAPON RACK\n<size=50%>coming soon</size>", new Vector3(0f, 2.15f, -0.24f), 1.8f, new Vector2(2.8f, 0.5f),
              new Color(1f, 0.92f, 0.6f), 0f);
    }

    // A big stone doorway with a glowing green middle. Walking into it teleports you.
    static Transform Portal(Transform parent, string name, Vector3 pos, float yaw, string sign)
    {
        var p = Group(parent, name, pos);
        p.rotation = Quaternion.Euler(0f, yaw, 0f);
        LBox(p, "Pillar", new Vector3(-1.9f, 2.2f, 0f), new Vector3(0.8f, 4.4f, 0.8f), stone);
        LBox(p, "Pillar", new Vector3(1.9f, 2.2f, 0f), new Vector3(0.8f, 4.4f, 0.8f), stone);
        LBox(p, "Top Stone", new Vector3(0f, 4.75f, 0f), new Vector3(4.8f, 0.7f, 0.9f), stone);
        NoCollider(LBox(p, "Rune Band", new Vector3(0f, 4.75f, -0.46f), new Vector3(3.6f, 0.4f, 0.02f), runes));
        NoCollider(LBox(p, "Portal Glow", new Vector3(0f, 2.05f, 0f), new Vector3(3f, 4.1f, 0.08f), portalGlow));
        var trigger = new GameObject("Portal Trigger");
        trigger.transform.SetParent(p, false);
        trigger.transform.localPosition = new Vector3(0f, 2f, 0f);
        var col = trigger.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3f, 4f, 1.2f);
        var tp = trigger.AddComponent<TemplePortal>();
        LLight(p, new Vector3(0f, 2.2f, -1f), FxSpawner.GoopGreen, 7f, 2f);
        LLabel(p, sign, new Vector3(0f, 5.6f, 0f), 2.4f, new Vector2(4.5f, 1f), FxSpawner.GoopGreen, 180f, faceCamera: true);
        return tp.transform;
    }

    // ================================================================ AREA 2: JUNGLE TEMPLE
    // Coordinates below are relative to the temple's center. The temple spans x -30..30, z -30..30.
    // Grid of rooms (north = +z):
    //
    //          x: -30 ... -12 | -12..-8 | -8 ... 8 | 8..12 | 12 ... 30
    //   z 12..30   GREAT STAIR    hall     (solid)    hall   CLIMBING SHAFT     <- floor 1
    //              UPPER GALLERY  hall   IDOL SANCTUM hall   TREASURE VAULT     <- floor 2 (y = 5)
    //   z  8..12   ------------------- hallway (both floors) -------------------
    //   z -8..8    CRYSTAL CAVE   hall   SUNKEN PIT   hall   LAVA BRIDGE        (pit room is 2 floors tall)
    //   z-12..-8   ------------------------- hallway ---------------------------
    //   z-30..-12  STOREROOM      hall   ENTRANCE     hall   SPIKE GALLERY
    //                                     (door south, portal outside)

    static void BuildTemple(Transform root, out Transform portal, out Transform arrival)
    {
        var shell = Group(root, "Temple Shell", root.position);

        // jungle ground + stone plaza outside the entrance
        Box(shell, "Jungle Ground", L(0f, -0.55f, 0f), new Vector3(170f, 1f, 170f), grass);
        Box(shell, "Entrance Plaza", L(0f, -0.48f, -40f), new Vector3(18f, 1f, 20f), stone);

        // ---- floor 1 (with a hole for the Sunken Pit)
        Box(shell, "Floor South", L(0f, -0.5f, -17.5f), new Vector3(60f, 1f, 25f), stone);
        Box(shell, "Floor North", L(0f, -0.5f, 17.5f), new Vector3(60f, 1f, 25f), stone);
        Box(shell, "Floor West", L(-17.5f, -0.5f, 0f), new Vector3(25f, 1f, 10f), stone);
        Box(shell, "Floor East", L(17.5f, -0.5f, 0f), new Vector3(25f, 1f, 10f), stone);

        // ---- outer walls (two floors tall), entrance door on the south side
        Wall(shell, "Outer Wall S", new Vector2(-30.5f, -30f), new Vector2(30.5f, -30f), 0f, Top + 0.5f, darkStone, 1f, D(0f, 4f, 4.5f));
        Wall(shell, "Outer Wall N", new Vector2(-30.5f, 30f), new Vector2(30.5f, 30f), 0f, Top + 0.5f, darkStone, 1f);
        Wall(shell, "Outer Wall W", new Vector2(-30f, -30f), new Vector2(-30f, 30f), 0f, Top + 0.5f, darkStone, 1f);
        Wall(shell, "Outer Wall E", new Vector2(30f, -30f), new Vector2(30f, 30f), 0f, Top + 0.5f, darkStone, 1f);
        Box(shell, "Roof", L(0f, Top + 0.25f, 0f), new Vector3(61f, 0.5f, 61f), darkStone);

        // ---- floor 2 slab over the north part (holes for the Great Stair ramp and the Climbing Shaft)
        float sy = F2 - 0.25f;
        Box(shell, "Upstairs Floor", L(0f, sy, 15f), new Vector3(60f, 0.5f, 14f), stone);          // z 8..22
        Box(shell, "Upstairs Floor", L(-26f, sy, 24f), new Vector3(8f, 0.5f, 4f), stone);           // z 22..26, x -30..-22
        Box(shell, "Upstairs Floor", L(5f, sy, 24f), new Vector3(38f, 0.5f, 4f), stone);            // z 22..26, x -14..24
        Box(shell, "Upstairs Floor", L(27f, sy, 23f), new Vector3(6f, 0.5f, 2f), stone);            // z 22..24, x 24..30
        Box(shell, "Upstairs Floor", L(-3f, sy, 28f), new Vector3(54f, 0.5f, 4f), stone);           // z 26..30, x -30..24
        Box(shell, "Foundation (under Idol Sanctum)", L(0f, F2 / 2f - 0.25f, 21f), new Vector3(16f, F2 - 0.5f, 18f), darkStone);
        // little walls so nobody walks off the open edges upstairs
        Wall(shell, "Ledge Wall", new Vector2(-12f, 8f), new Vector2(-8f, 8f), F2, 1f, stone, 0.3f);
        Wall(shell, "Ledge Wall", new Vector2(8f, 8f), new Vector2(12f, 8f), F2, 1f, stone, 0.3f);

        // ---- the rooms
        var rooms = Group(root, "Rooms", root.position);
        EntranceHall(Group(rooms, "Entrance Hall", root.position));
        Storeroom(Group(rooms, "Mossy Storeroom", root.position));
        SpikeGallery(Group(rooms, "Spike Gallery", root.position));
        CrystalCave(Group(rooms, "Crystal Cave", root.position));
        LavaBridge(Group(rooms, "Lava Bridge Room", root.position));
        SunkenPit(Group(rooms, "Sunken Pit (center)", root.position));
        GreatStair(Group(rooms, "Great Stair", root.position));
        ClimbingShaft(Group(rooms, "Climbing Shaft", root.position));
        UpperGallery(Group(rooms, "Upper Gallery (floor 2)", root.position));
        IdolSanctum(Group(rooms, "Idol Sanctum (floor 2, deepest)", root.position));
        TreasureVault(Group(rooms, "Treasure Vault (floor 2)", root.position));

        // a few torches in the hallways
        var halls = Group(root, "Hallway Torches", root.position);
        Torch(halls, L(-10f, 2.6f, -29.6f), 0f, false);
        Torch(halls, L(10f, 2.6f, -29.6f), 0f, false);
        Torch(halls, L(-29.6f, 2.6f, -10f), 90f, false);
        Torch(halls, L(29.6f, 2.6f, -10f), -90f, false);
        Torch(halls, L(-29.6f, 2.6f, 10f), 90f, true);
        Torch(halls, L(29.6f, 2.6f, 10f), -90f, true);
        Torch(halls, L(-10f, F2 + 2.6f, 29.6f), 180f, false);
        Torch(halls, L(10f, F2 + 2.6f, 29.6f), 180f, false);

        // ---- outside: big stone face over the door, pillars, torches, a few trees, and the portal home
        var outside = Group(root, "Temple Outside", root.position);
        Box(outside, "Door Pillar", L(-3.2f, 3f, -31f), new Vector3(1.2f, 6f, 1.2f), stone);
        Box(outside, "Door Pillar", L(3.2f, 3f, -31f), new Vector3(1.2f, 6f, 1.2f), stone);
        Box(outside, "Stone Face", L(0f, 7.8f, -30.9f), new Vector3(7f, 3.6f, 1f), stone);
        NoCollider(Box(outside, "Face Eye", L(-1.6f, 8.3f, -31.45f), new Vector3(1f, 0.6f, 0.1f), portalGlow));
        NoCollider(Box(outside, "Face Eye", L(1.6f, 8.3f, -31.45f), new Vector3(1f, 0.6f, 0.1f), portalGlow));
        NoCollider(Box(outside, "Face Mouth", L(0f, 6.7f, -31.45f), new Vector3(3f, 0.4f, 0.1f), darkStone));
        NoCollider(Box(outside, "Rune Band", L(0f, 9.85f, -31.05f), new Vector3(10f, 0.6f, 0.1f), runes));
        Torch(outside, L(-4.2f, 2.8f, -31.7f), 180f, true);
        Torch(outside, L(4.2f, 2.8f, -31.7f), 180f, false);
        for (int i = 0; i < 6; i++)   // vines hanging on the front wall
            NoCollider(Box(outside, "Vine", L(-25f + i * 9.5f + (i % 2) * 2f, 7f - (i % 3), -30.55f), new Vector3(0.25f, 5f + (i % 3) * 1.5f, 0.08f), moss));
        foreach (var p in new[] { new Vector2(-22, -45), new Vector2(20, -48), new Vector2(-40, -20), new Vector2(42, -10),
                                  new Vector2(-45, 25), new Vector2(40, 35) })
            GiantTree(outside, L(p.x, 0f, p.y), 1.1f, 20f);

        portal = Portal(outside, "Portal Home", L(0f, 0f, -46f), 180f, "BACK TO\nTREEHOUSE BASE");
        arrival = new GameObject("Arrival At Temple").transform;
        arrival.SetParent(root, false);
        arrival.SetPositionAndRotation(L(0f, 0.05f, -41f), Quaternion.identity); // facing the temple door
    }

    // ---------------------------------------------------------------- rooms (floor 1)

    static void EntranceHall(Transform r)
    {
        Wall(r, "Wall", new Vector2(-8f, -30f), new Vector2(-8f, -12f), 0f, Top, stone, WallT, D(-16f, 3f));
        Wall(r, "Wall", new Vector2(8f, -30f), new Vector2(8f, -12f), 0f, Top, stone, WallT, D(-16f, 3f));
        Wall(r, "Wall", new Vector2(-8f, -12f), new Vector2(8f, -12f), 0f, Top, stone, WallT, D(0f, 4f, 4f));
        foreach (var x in new[] { -4.5f, 4.5f })
            foreach (var z in new[] { -25f, -18f })
                Pillar(r, L(x, 0f, z));
        NoCollider(Box(r, "Rune Panel", L(-7.65f, 3f, -21f), new Vector3(0.05f, 2.5f, 4f), runes));
        NoCollider(Box(r, "Rune Panel", L(7.65f, 3f, -21f), new Vector3(0.05f, 2.5f, 4f), runes));
        Torch(r, L(-7.6f, 2.6f, -26f), 90f, true);
        Torch(r, L(7.6f, 2.6f, -26f), -90f, false);
        Torch(r, L(-7.6f, 2.6f, -14f), 90f, false);
        Torch(r, L(7.6f, 2.6f, -14f), -90f, false);
        Loot(r, L(-6f, 0f, -28f), 1);
        Loot(r, L(6f, 0f, -14f), 1);
    }

    static void Storeroom(Transform r)
    {
        Wall(r, "Wall", new Vector2(-12f, -30f), new Vector2(-12f, -12f), 0f, Top, stone, WallT, D(-24f, 3f));
        Wall(r, "Wall", new Vector2(-30f, -12f), new Vector2(-12f, -12f), 0f, Top, stone, WallT, D(-21f, 3f));
        var rng = new System.Random(7);
        for (int i = 0; i < 9; i++)   // old crates and pots
        {
            var p = L(-27f + (float)rng.NextDouble() * 12f, 0f, -28f + (float)rng.NextDouble() * 13f);
            if (i % 3 == 2) Cyl(r, "Pot", p + Vector3.up * 0.45f, 0.35f, 0.9f, darkWood);
            else Box(r, "Crate", p + Vector3.up * 0.5f, Vector3.one * (0.8f + (float)rng.NextDouble() * 0.4f), wood, new Vector3(0f, i * 23f, 0f));
        }
        NoCollider(Box(r, "Moss", L(-21f, 0.01f, -21f), new Vector3(10f, 0.02f, 8f), moss));
        Torch(r, L(-29.6f, 2.6f, -21f), 90f, true);
        Loot(r, L(-28f, 0f, -28f), 1);
        Loot(r, L(-14f, 0f, -26f), 1);
        Loot(r, L(-20f, 0f, -14f), 1);
        Monster(r, L(-21f, 0f, -21f), "Mossy Storeroom");
    }

    static void SpikeGallery(Transform r)
    {
        Wall(r, "Wall", new Vector2(12f, -30f), new Vector2(12f, -12f), 0f, Top, stone, WallT, D(-24f, 3f));
        Wall(r, "Wall", new Vector2(12f, -12f), new Vector2(30f, -12f), 0f, Top, stone, WallT, D(21f, 3f));
        Spikes(r, L(17.5f, 0f, -21f), 3f, 17.4f, 0f);       // two strips of spikes to time your way across
        Spikes(r, L(24.5f, 0f, -21f), 3f, 17.4f, 1.6f);
        Torch(r, L(29.6f, 2.6f, -21f), -90f, true);
        Loot(r, L(28f, 0f, -28f), 1);
        Loot(r, L(28f, 0f, -14f), 1);
        Loot(r, L(21f, 0f, -28.5f), 1);
        Monster(r, L(21f, 0f, -18f), "Spike Gallery");
    }

    static void CrystalCave(Transform r)
    {
        Wall(r, "Wall", new Vector2(-30f, -8f), new Vector2(-12f, -8f), 0f, Top, stone, WallT, D(-25f, 3f));
        Wall(r, "Wall", new Vector2(-12f, -8f), new Vector2(-12f, 8f), 0f, Top, stone, WallT, D(0f, 3f));
        Wall(r, "Wall", new Vector2(-30f, 8f), new Vector2(-12f, 8f), 0f, Top, stone, WallT, D(-21f, 3f));
        // dark: no torches here, only glowing crystals
        Crystals(r, L(-26f, 0f, -4f), crystalCyan, new Color(0.3f, 0.9f, 1f), true);
        Crystals(r, L(-17f, 0f, 4.5f), crystalPurple, new Color(0.7f, 0.35f, 1f), true);
        Crystals(r, L(-28f, 0f, 6f), crystalPurple, Color.clear, false);
        Crystals(r, L(-15f, 0f, -5.5f), crystalCyan, Color.clear, false);
        Crystals(r, L(-22f, 0f, 0.5f), crystalCyan, Color.clear, false);
        var rng = new System.Random(3);
        for (int i = 0; i < 6; i++)   // rock lumps
            Box(r, "Rock", L(-28f + i * 2.8f, 0.4f, (i % 2 == 0 ? -6.5f : 6.5f)), Vector3.one * (1f + (float)rng.NextDouble()), darkStone,
                new Vector3(i * 17f, i * 41f, i * 9f));
        Loot(r, L(-27f, 0f, -6f), 2);
        Loot(r, L(-18f, 0f, 6f), 2);
        Loot(r, L(-22f, 0f, -2f), 2);
        Monster(r, L(-21f, 0f, 0f), "Crystal Cave");
    }

    static void LavaBridge(Transform r)
    {
        Wall(r, "Wall", new Vector2(12f, -8f), new Vector2(12f, 8f), 0f, Top, stone, WallT, D(0f, 3f));
        Wall(r, "Wall", new Vector2(12f, -8f), new Vector2(30f, -8f), 0f, Top, stone, WallT);
        Wall(r, "Wall", new Vector2(12f, 8f), new Vector2(30f, 8f), 0f, Top, stone, WallT);
        Lava(r, L(21.85f, 0f, 0f), new Vector3(15.7f, 0f, 15.4f), 0f);
        Box(r, "Stone Bridge", L(19f, 0.2f, 0f), new Vector3(14f, 0.4f, 1.4f), stone);
        Box(r, "Treasure Island", L(27.85f, 0.2f, 0f), new Vector3(3.7f, 0.4f, 4f), stone);
        Box(r, "Stepping Stone", L(19f, 0.2f, 3f), new Vector3(1.2f, 0.4f, 1.2f), stone);
        Box(r, "Stepping Stone", L(20.6f, 0.2f, 5f), new Vector3(1.2f, 0.4f, 1.2f), stone);
        Box(r, "Stepping Stone", L(22.4f, 0.2f, 6.4f), new Vector3(1.2f, 0.4f, 1.2f), stone);
        PointLight(r, L(21f, 1.5f, 0f), new Color(1f, 0.45f, 0.15f), 14f, 2.5f);
        Loot(r, L(27.5f, 0.4f, -1f), 2);
        Loot(r, L(28.5f, 0.4f, 1f), 2);
        Loot(r, L(22.4f, 0.4f, 6.4f), 2);
        Monster(r, L(14f, 0f, -5f), "Lava Bridge Room");
    }

    static void SunkenPit(Transform r)
    {
        // walls: two floors tall; floor-2 doors in the north wall where the elevators arrive
        Wall(r, "Wall", new Vector2(-8f, -8f), new Vector2(8f, -8f), 0f, Top, stone, WallT, D(0f, 4f, 4f));
        Wall(r, "Wall", new Vector2(-8f, -8f), new Vector2(-8f, 8f), 0f, Top, stone, WallT, D(0f, 3f));
        Wall(r, "Wall", new Vector2(8f, -8f), new Vector2(8f, 8f), 0f, Top, stone, WallT, D(0f, 3f));
        Wall(r, "Wall", new Vector2(-8f, 8f), new Vector2(8f, 8f), 0f, F2, stone, WallT, D(0f, 3f));
        Wall(r, "Wall Upper", new Vector2(-8f, 8f), new Vector2(8f, 8f), F2, Top - F2, stone, WallT, D(-6.3f, 3f), D(6.3f, 3f));

        // the pit: 10 x 10 m, 3 m deep, lava at the bottom
        Box(r, "Pit Bottom", L(0f, -3.5f, 0f), new Vector3(11f, 1f, 11f), darkStone);
        Box(r, "Pit Wall", L(0f, -2f, 5.5f), new Vector3(12f, 2f, 1f), darkStone);
        Box(r, "Pit Wall", L(0f, -2f, -5.5f), new Vector3(12f, 2f, 1f), darkStone);
        Box(r, "Pit Wall", L(5.5f, -2f, 0f), new Vector3(1f, 2f, 10f), darkStone);
        Box(r, "Pit Wall", L(-5.5f, -2f, 0f), new Vector3(1f, 2f, 10f), darkStone);
        Lava(r, L(0f, -3f, 0f), new Vector3(10f, 0f, 10f), 1.2f);
        Box(r, "Island Pillar", L(0f, -1.5f, 0f), new Vector3(2f, 3f, 2f), stone);
        NoCollider(Box(r, "Island Runes", L(0f, -0.6f, -1.01f), new Vector3(1.6f, 1f, 0.02f), runes));

        // sliding stone platforms out to the island (they carry you)
        Mover(r, L(-4f, -0.15f, 0f), new Vector3(2f, 0f, 0f), 0f);
        Mover(r, L(4f, -0.15f, 0f), new Vector3(-2f, 0f, 0f), 1.3f);
        Mover(r, L(0f, -0.15f, -4f), new Vector3(0f, 0f, 2f), 2.6f);

        // two elevators (our Moving_Platform prefab: rises 5 m) up to the floor-2 doors
        Elevator(r, L(-6.3f, -0.125f, 6.3f));
        Elevator(r, L(6.3f, -0.125f, 6.3f));

        foreach (var c in new[] { new Vector2(-7f, -7f), new Vector2(7f, -7f) })
            Box(r, "Corner Column", L(c.x, Top / 2f, c.y), new Vector3(1.2f, Top, 1.2f), stone);
        Crystals(r, L(0.5f, 0f, 0.4f), crystalCyan, new Color(0.3f, 0.9f, 1f), true, 0.6f);
        Torch(r, L(-7.6f, 2.6f, -4f), 90f, false);
        Torch(r, L(7.6f, 2.6f, -4f), -90f, false);
        Loot(r, L(-0.5f, 0f, -0.4f), 3);    // on the island
        Loot(r, L(0f, 0f, 6.8f), 2);        // north ledge
        Monster(r, L(-6f, 0f, -6f), "Sunken Pit");
    }

    static void GreatStair(Transform r)
    {
        Wall(r, "Wall", new Vector2(-30f, 12f), new Vector2(-12f, 12f), 0f, F2 - 0.5f, stone, WallT, D(-21f, 3f));
        Wall(r, "Wall", new Vector2(-12f, 12f), new Vector2(-12f, 30f), 0f, F2 - 0.5f, stone, WallT, D(20f, 3f));
        // ramp from the floor (x -29) up to floor 2 (x -14)
        float len = Mathf.Sqrt(15f * 15f + F2 * F2);
        float angle = Mathf.Atan2(F2, 15f) * Mathf.Rad2Deg;
        Vector3 mid = L(-21.5f, F2 / 2f, 24f);
        Vector3 down = Quaternion.Euler(0f, 0f, angle) * Vector3.up * -0.2f;
        Box(r, "Ramp", mid + down, new Vector3(len, 0.4f, 4f), stone, new Vector3(0f, 0f, angle));
        Box(r, "Ramp Side", mid + down + new Vector3(0f, 0.3f, -2.1f), new Vector3(len, 0.25f, 0.2f), darkStone, new Vector3(0f, 0f, angle));
        // rails around the opening upstairs
        Wall(r, "Rail", new Vector2(-22f, 22f), new Vector2(-14f, 22f), F2, 1f, stone, 0.3f);
        Wall(r, "Rail", new Vector2(-22f, 26f), new Vector2(-14f, 26f), F2, 1f, stone, 0.3f);
        Wall(r, "Rail", new Vector2(-22f, 22f), new Vector2(-22f, 26f), F2, 1f, stone, 0.3f);
        Torch(r, L(-29.6f, 2.6f, 16f), 90f, true);
        Torch(r, L(-21f, 2.6f, 29.6f), 180f, false);
        Loot(r, L(-27f, 0f, 15f), 2);
        Loot(r, L(-25f, 0f, 21f), 2);   // tucked beside the ramp
        Monster(r, L(-18f, 0f, 16f), "Great Stair");
    }

    static void ClimbingShaft(Transform r)
    {
        Wall(r, "Wall", new Vector2(12f, 12f), new Vector2(30f, 12f), 0f, F2 - 0.5f, stone, WallT, D(21f, 3f));
        Wall(r, "Wall", new Vector2(12f, 12f), new Vector2(12f, 30f), 0f, F2 - 0.5f, stone, WallT, D(20f, 3f));
        // stone blocks you can jump up (or climb the walls with grip) to the hole in the ceiling
        Box(r, "Climb Block 1", L(28.5f, 0.6f, 28.5f), new Vector3(2.4f, 1.2f, 2.4f), stone);
        Box(r, "Climb Block 2", L(28.5f, 1.2f, 25.3f), new Vector3(2.4f, 2.4f, 2.4f), stone);
        Box(r, "Climb Block 3", L(25.5f, 1.8f, 25.3f), new Vector3(2.4f, 3.6f, 2.4f), stone);
        Box(r, "Climb Block 4", L(25.2f, 2.4f, 28.5f), new Vector3(2.4f, 4.8f, 2.4f), stone);
        NoCollider(Box(r, "Climb Runes", L(29.65f, 2.5f, 26.9f), new Vector3(0.05f, 4f, 5f), runes));
        for (int i = 0; i < 5; i++)   // vines on the climbing walls
            NoCollider(Box(r, "Vine", L(29.62f, 2.5f, 24.5f + i * 1.2f), new Vector3(0.05f, 4.6f - i * 0.5f, 0.2f), moss));
        Torch(r, L(21f, 2.6f, 29.6f), 180f, true);
        Loot(r, L(15f, 0f, 27f), 2);
        Loot(r, L(28.5f, 1.2f, 28.5f), 2);   // on top of the first climb block
    }

    // ---------------------------------------------------------------- rooms (floor 2)

    static void UpperGallery(Transform r)
    {
        Wall(r, "Wall", new Vector2(-30f, 12f), new Vector2(-12f, 12f), F2, Top - F2, stone, WallT, D(-26f, 3f));
        Wall(r, "Wall", new Vector2(-12f, 12f), new Vector2(-12f, 30f), F2, Top - F2, stone, WallT, D(20f, 3f));
        // broken statues
        Box(r, "Statue Base", L(-27f, F2 + 0.4f, 16f), new Vector3(1.4f, 0.8f, 1.4f), darkStone);
        Box(r, "Statue Body", L(-27f, F2 + 1.6f, 16f), new Vector3(0.9f, 1.6f, 0.7f), stone);
        Ball(r, "Statue Head (fallen)", L(-25.5f, F2 + 0.35f, 17.5f), Vector3.one * 0.7f, stone);
        Box(r, "Statue Base", L(-27f, F2 + 0.4f, 28f), new Vector3(1.4f, 0.8f, 1.4f), darkStone);
        Box(r, "Statue Body", L(-27f, F2 + 1.3f, 28f), new Vector3(0.9f, 1f, 0.7f), stone, new Vector3(0f, 0f, 12f));
        NoCollider(Box(r, "Rune Panel", L(-29.6f, F2 + 2.5f, 21f), new Vector3(0.05f, 2.5f, 5f), runes));
        Torch(r, L(-29.6f, F2 + 2.6f, 25.5f), 90f, true);
        Loot(r, L(-28f, F2, 20f), 3);
        Loot(r, L(-15f, F2, 29f), 3);
        Monster(r, L(-20f, F2, 16f), "Upper Gallery");
    }

    static void IdolSanctum(Transform r)
    {
        Wall(r, "Wall", new Vector2(-8f, 12f), new Vector2(8f, 12f), F2, Top - F2, darkStone, WallT, D(0f, 3f));
        Wall(r, "Wall", new Vector2(-8f, 12f), new Vector2(-8f, 30f), F2, Top - F2, darkStone, WallT);
        Wall(r, "Wall", new Vector2(8f, 12f), new Vector2(8f, 30f), F2, Top - F2, darkStone, WallT);
        // guarded by spikes, then lava with stepping stones, then the golden altar
        Spikes(r, L(0f, F2, 14.6f), 15.4f, 3f, 0.8f);
        Lava(r, L(0f, F2, 20.5f), new Vector3(15.4f, 0f, 5f), 0f);
        Box(r, "Stepping Stone", L(-1.5f, F2 + 0.2f, 18.8f), new Vector3(1.3f, 0.4f, 1.3f), stone);
        Box(r, "Stepping Stone", L(0.8f, F2 + 0.2f, 20.4f), new Vector3(1.3f, 0.4f, 1.3f), stone);
        Box(r, "Stepping Stone", L(-0.6f, F2 + 0.2f, 22.1f), new Vector3(1.3f, 0.4f, 1.3f), stone);
        Box(r, "Altar Step", L(0f, F2 + 0.2f, 26.5f), new Vector3(6f, 0.4f, 6f), darkStone);
        Box(r, "Altar", L(0f, F2 + 0.75f, 27f), new Vector3(2f, 0.7f, 1.4f), stone);
        NoCollider(Box(r, "Altar Gold Trim", L(0f, F2 + 1.12f, 27f), new Vector3(2.1f, 0.05f, 1.5f), gold));
        NoCollider(Box(r, "Rune Wall", L(0f, F2 + 2.5f, 29.65f), new Vector3(10f, 4f, 0.05f), runes));
        Crystals(r, L(-6.5f, F2, 28.5f), crystalPurple, Color.clear, false);
        Crystals(r, L(6.5f, F2, 28.5f), crystalPurple, Color.clear, false);
        PointLight(r, L(0f, F2 + 3f, 25f), new Color(1f, 0.75f, 0.3f), 12f, 2.2f);
        Loot(r, L(0f, F2 + 1.1f, 27f), 4);   // the Temple Idol
    }

    static void TreasureVault(Transform r)
    {
        Wall(r, "Wall", new Vector2(12f, 12f), new Vector2(30f, 12f), F2, Top - F2, stone, WallT, D(16f, 3f));
        Wall(r, "Wall", new Vector2(12f, 12f), new Vector2(12f, 30f), F2, Top - F2, stone, WallT, D(20f, 3f));
        // rails around the Climbing Shaft hole (open on the side where the climb blocks are)
        Wall(r, "Rail", new Vector2(24f, 24f), new Vector2(30f, 24f), F2, 1f, stone, 0.3f);
        Wall(r, "Rail", new Vector2(24f, 24f), new Vector2(24f, 27f), F2, 1f, stone, 0.3f);
        for (int i = 0; i < 3; i++)   // treasure chests
        {
            var c = L(15f + i * 3f, F2, 28.6f);
            Box(r, "Chest", c + Vector3.up * 0.35f, new Vector3(1.2f, 0.7f, 0.8f), wood);
            NoCollider(Box(r, "Chest Lid", c + Vector3.up * 0.75f, new Vector3(1.25f, 0.12f, 0.85f), gold));
        }
        for (int i = 0; i < 4; i++)   // gold piles (decoration)
            NoCollider(Cyl(r, "Gold Pile", L(14f + i * 2.2f, F2 + 0.1f, 15f + (i % 2) * 1.5f), 0.5f, 0.2f, gold));
        Torch(r, L(29.6f, F2 + 2.6f, 18f), -90f, true);
        Loot(r, L(20f, F2, 21f), 3);
        Loot(r, L(28f, F2, 15f), 3);
        Monster(r, L(18f, F2, 18f), "Treasure Vault");
    }

    // ---------------------------------------------------------------- temple pieces

    static void Pillar(Transform r, Vector3 basePos)
    {
        Box(r, "Pillar Base", basePos + Vector3.up * 0.2f, new Vector3(1.3f, 0.4f, 1.3f), darkStone);
        Cyl(r, "Pillar", basePos + Vector3.up * (Top / 2f), 0.45f, Top, stone);
        NoCollider(Box(r, "Pillar Moss", basePos + new Vector3(0.36f, 2f, 0f), new Vector3(0.12f, 3f, 0.5f), moss));
    }

    // Flat lava that kills (KillZone). "riseAbove" = how tall the deadly area is above the surface.
    static void Lava(Transform r, Vector3 center, Vector3 size, float extraHeight)
    {
        NoCollider(Box(r, "Lava", center + Vector3.up * 0.02f, new Vector3(size.x, 0.04f, size.z), lavaGlow));
        var zone = new GameObject("Lava (Kill Zone)");
        zone.transform.SetParent(r, false);
        zone.transform.position = center + Vector3.up * (0.15f + extraHeight / 2f);
        var col = zone.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(size.x, 0.3f + extraHeight, size.z);
        zone.AddComponent<KillZone>();
        MakeStatic(zone);
    }

    static void Spikes(Transform r, Vector3 center, float width, float depth, float timeOffset)
    {
        var trap = new GameObject("Spike Floor");
        trap.transform.SetParent(r, false);
        trap.transform.position = center;
        var col = trap.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 0.5f, 0f);
        col.size = new Vector3(width, 1f, depth);
        NoCollider(Box(trap.transform, "Spike Plate", center + Vector3.up * 0.01f, new Vector3(width, 0.02f, depth), darkStone));

        var spikes = new GameObject("Spikes");
        spikes.transform.SetParent(trap.transform, false);
        spikes.transform.localPosition = new Vector3(0f, -0.45f, 0f);
        spikes.AddComponent<MeshFilter>().sharedMesh = SpikeMesh(width, depth);
        var mr = spikes.AddComponent<MeshRenderer>();
        mr.sharedMaterial = metal;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var floor = trap.AddComponent<SpikeFloor>();
        floor.spikes = spikes.transform;
        floor.timeOffset = timeOffset;
    }

    // All the spikes for one trap merged into ONE mesh = one draw call (much faster on Quest than hundreds of cubes).
    static Mesh SpikeMesh(float width, float depth)
    {
        string path = $"{GenFolder}/Spikes_{width:0.#}x{depth:0.#}.asset".Replace(',', '.');
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;

        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cube = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);

        var parts = new List<CombineInstance>();
        const float step = 0.5f;
        int nx = Mathf.Max(1, Mathf.FloorToInt(width / step));
        int nz = Mathf.Max(1, Mathf.FloorToInt(depth / step));
        for (int ix = 0; ix < nx; ix++)
            for (int iz = 0; iz < nz; iz++)
            {
                float x = -width / 2f + step * (ix + 0.5f);
                float z = -depth / 2f + step * (iz + 0.5f);
                parts.Add(new CombineInstance                                    // the stake
                {
                    mesh = cube,
                    transform = Matrix4x4.TRS(new Vector3(x, 0.2f, z), Quaternion.Euler(0f, 45f, 0f), new Vector3(0.07f, 0.4f, 0.07f)),
                });
            }
        var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.CombineMeshes(parts.ToArray(), true, true);
        mesh.RecalculateBounds();
        EnsureFolder(GenFolder);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static void Mover(Transform r, Vector3 pos, Vector3 offset, float timeOffset)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Sliding Platform";
        go.transform.SetParent(r, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2f, 0.3f, 2f);
        go.GetComponent<Renderer>().sharedMaterial = moss;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        var m = go.AddComponent<TempleMover>();
        m.moveOffset = offset;
        m.secondsPerTrip = 3f;
        m.pauseAtEnds = 1.2f;
        m.timeOffset = timeOffset;
        // NOT static: it moves
    }

    static void Elevator(Transform r, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ElevatorPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[Jungle Temple] Moving_Platform prefab not found, using a sliding platform instead.");
            Mover(r, pos + Vector3.up * 0.0f, new Vector3(0f, F2, 0f), 0f);
            return;
        }
        var e = (GameObject)PrefabUtility.InstantiatePrefab(prefab, r);
        e.name = "Elevator (Moving_Platform)";
        e.transform.SetPositionAndRotation(pos, Quaternion.identity);
    }

    static void Torch(Transform r, Vector3 wallPos, float yaw, bool withLight)
    {
        var t = Group(r, "Torch", wallPos);
        t.rotation = Quaternion.Euler(0f, yaw, 0f);   // +Z points out of the wall
        NoCollider(Box(t, "Bracket", wallPos + t.rotation * new Vector3(0f, -0.25f, 0.05f), new Vector3(0.12f, 0.12f, 0.2f), metal, new Vector3(0f, yaw, 0f)));
        NoCollider(Cyl(t, "Stick", wallPos + t.rotation * new Vector3(0f, -0.05f, 0.18f), 0.05f, 0.5f, darkWood, new Vector3(-25f, yaw, 0f)));
        var flame = NoCollider(Box(t, "Flame", wallPos + t.rotation * new Vector3(0f, 0.25f, 0.28f), Vector3.one * 0.16f, torchGlow, new Vector3(45f, yaw + 45f, 0f)));
        flame.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (withLight) PointLight(t, wallPos + t.rotation * new Vector3(0f, 0.3f, 0.8f), new Color(1f, 0.6f, 0.25f), 10f, 2.2f);
    }

    static void Crystals(Transform r, Vector3 basePos, Material mat, Color lightColor, bool withLight, float scale = 1f)
    {
        var c = Group(r, "Glowing Crystals", basePos);
        var rng = new System.Random(Mathf.RoundToInt(basePos.x * 13f + basePos.z * 7f));
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f + (float)rng.NextDouble() * 30f;
            float h = (0.6f + (float)rng.NextDouble() * 0.9f) * scale;
            Vector3 off = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, (i == 0 ? 0f : 0.35f) * scale);
            var shard = Box(c, "Crystal", basePos + off + Vector3.up * h * 0.4f, new Vector3(0.22f * scale, h, 0.22f * scale), mat,
                            new Vector3((i == 0 ? 0f : 20f), a, (i == 0 ? 0f : 15f)));
            shard.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            NoCollider(shard);
        }
        if (withLight) PointLight(c, basePos + Vector3.up * 1.2f, lightColor, 9f, 2.5f);
    }

    static void Loot(Transform r, Vector3 pos, int tier)
    {
        var p = new GameObject("Loot Spawn (tier " + tier + ")");
        p.transform.SetParent(r, false);
        p.transform.position = pos + Vector3.up * 0.02f;
        p.AddComponent<LootSpawnPoint>().tier = tier;
    }

    static void Monster(Transform r, Vector3 pos, string room)
    {
        var m = new GameObject("MonsterSpawn - " + room);
        m.transform.SetParent(r, false);
        m.transform.position = pos;
        m.AddComponent<MonsterSpawn>().roomName = room;
    }

    // ================================================================ building helpers

    // temple-local position -> world position
    static Vector3 L(float x, float y, float z) => TempleOrigin + new Vector3(x, y, z);

    struct Door
    {
        public float at, width, height;
    }

    // a door in a wall: "at" is the x (for east-west walls) or z (for north-south walls) of the door's middle
    static Door D(float at, float width, float height = 3.5f) => new Door { at = at, width = width, height = height };

    // A straight wall along x or z (temple-local coords), from y0 up "height" meters, with door holes.
    static void Wall(Transform parent, string name, Vector2 a, Vector2 b, float y0, float height, Material mat, float thickness,
                     params Door[] doors)
    {
        bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.y - a.y);
        float start = alongX ? Mathf.Min(a.x, b.x) : Mathf.Min(a.y, b.y);
        float end = alongX ? Mathf.Max(a.x, b.x) : Mathf.Max(a.y, b.y);
        float fixedCoord = alongX ? a.y : a.x;

        void Piece(float s, float e, float py, float ph)
        {
            if (e - s < 0.01f || ph < 0.01f) return;
            float m = (s + e) / 2f;
            Vector3 c = alongX ? L(m, py + ph / 2f, fixedCoord) : L(fixedCoord, py + ph / 2f, m);
            Vector3 size = alongX ? new Vector3(e - s, ph, thickness) : new Vector3(thickness, ph, e - s);
            Box(parent, name, c, size, mat);
        }

        var sorted = new List<Door>(doors);
        sorted.Sort((x, y) => x.at.CompareTo(y.at));
        float cursor = start;
        foreach (var d in sorted)
        {
            float ds = d.at - d.width / 2f, de = d.at + d.width / 2f;
            Piece(cursor, ds, y0, height);
            Piece(ds, de, y0 + d.height, height - d.height);   // the part above the door
            cursor = de;
        }
        Piece(cursor, end, y0, height);
    }

    static Transform Group(Transform parent, string name, Vector3 position)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        g.position = position;
        return g;
    }

    static GameObject Box(Transform parent, string name, Vector3 worldPos, Vector3 size, Material mat, Vector3? euler = null)
        => Shape(PrimitiveType.Cube, parent, name, worldPos, size, mat, euler);

    static GameObject Ball(Transform parent, string name, Vector3 worldPos, Vector3 size, Material mat)
        => Shape(PrimitiveType.Sphere, parent, name, worldPos, size, mat, null);

    // cylinder by radius + height
    static GameObject Cyl(Transform parent, string name, Vector3 worldPos, float radius, float height, Material mat, Vector3? euler = null)
        => Shape(PrimitiveType.Cylinder, parent, name, worldPos, new Vector3(radius * 2f, height / 2f, radius * 2f), mat, euler);

    static GameObject Shape(PrimitiveType type, Transform parent, string name, Vector3 worldPos, Vector3 size, Material mat, Vector3? euler)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.Euler(euler ?? Vector3.zero);
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        MakeStatic(go);
        return go;
    }

    // ---- same as Box/Cyl/Ball/Label/PointLight, but the position + rotation are relative to the parent (for props)
    static GameObject LBox(Transform p, string name, Vector3 localPos, Vector3 size, Material mat, Vector3? localEuler = null)
        => Local(Box(p, name, Vector3.zero, size, mat), localPos, localEuler);

    static GameObject LBall(Transform p, string name, Vector3 localPos, Vector3 size, Material mat)
        => Local(Ball(p, name, Vector3.zero, size, mat), localPos, null);

    static GameObject LCyl(Transform p, string name, Vector3 localPos, float radius, float height, Material mat, Vector3? localEuler = null)
        => Local(Cyl(p, name, Vector3.zero, radius, height, mat), localPos, localEuler);

    static GameObject Local(GameObject go, Vector3 localPos, Vector3? localEuler)
    {
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler ?? Vector3.zero);
        return go;
    }

    static TextMeshPro LLabel(Transform p, string text, Vector3 localPos, float size, Vector2 box, Color color, float yaw,
                              bool faceCamera = false)
    {
        var tmp = Label(p, text, Vector3.zero, size, box, color, yaw, faceCamera);
        tmp.transform.localPosition = localPos;
        tmp.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 180f, 0f);
        return tmp;
    }

    static void LLight(Transform p, Vector3 localPos, Color color, float range, float intensity)
        => PointLight(p, p.TransformPoint(localPos), color, range, intensity);

    static GameObject NoCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        return go;
    }

    // Static = never moves. Unity can then batch them together (fewer draw calls) and cull hidden ones.
    static void MakeStatic(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
    }

    static void PointLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        l.lightmapBakeType = LightmapBakeType.Realtime;
    }

    static TextMeshPro Label(Transform parent, string text, Vector3 worldPos, float size, Vector2 box, Color color, float yaw,
                             bool faceCamera = false)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 180f, 0f));
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.rectTransform.sizeDelta = box;
        if (faceCamera) go.AddComponent<FaceCamera>();
        return tmp;
    }

    // ================================================================ materials

    static void LoadMaterials()
    {
        EnsureFolder(MatFolder);
        grass = Existing("Material_Grass");
        runes = Existing("Material_Runes");
        gold = Existing("Material_GoldGlow");
        stripes = Existing("Material_Stripes");

        // new colors made by copying our existing simple material (same shader) - see Assets/Materials/JungleTemple
        wood = Copy("JT_Wood", "Material_Simple_Orange", new Color(0.52f, 0.33f, 0.17f));
        darkWood = Copy("JT_DarkWood", "Material_Simple_Orange", new Color(0.32f, 0.19f, 0.09f));
        bark = Copy("JT_Bark", "Material_Simple_Orange", new Color(0.38f, 0.26f, 0.15f));
        leaves = Copy("JT_Leaves", "Material_Simple_Green", new Color(0.18f, 0.5f, 0.2f));
        rope = Copy("JT_Rope", "Material_Simple_Yellow", new Color(0.78f, 0.66f, 0.42f));
        stone = Copy("JT_Stone", "Material_Simple_BlueLight", new Color(0.52f, 0.5f, 0.45f));
        darkStone = Copy("JT_DarkStone", "Material_Simple_BlueDark", new Color(0.3f, 0.3f, 0.28f));
        moss = Copy("JT_Moss", "Material_Simple_Green", new Color(0.26f, 0.42f, 0.2f));
        metal = Copy("JT_Metal", "Material_Simple_BlueLight", new Color(0.62f, 0.63f, 0.66f));

        // glowing (emissive) materials: bright without needing real lights
        portalGlow = Copy("JT_PortalGlow", "Material_GoldGlow", new Color(0.25f, 1f, 0.4f), new Color(0.2f, 1.6f, 0.45f));
        lavaGlow = Copy("JT_LavaGlow", "Material_GoldGlow", new Color(1f, 0.35f, 0.05f), new Color(2f, 0.55f, 0.05f));
        torchGlow = Copy("JT_TorchGlow", "Material_GoldGlow", new Color(1f, 0.65f, 0.15f), new Color(2.2f, 1.1f, 0.25f));
        crystalCyan = Copy("JT_CrystalCyan", "Material_GoldGlow", new Color(0.4f, 0.95f, 1f), new Color(0.3f, 1.4f, 1.8f));
        crystalPurple = Copy("JT_CrystalPurple", "Material_GoldGlow", new Color(0.75f, 0.45f, 1f), new Color(1f, 0.45f, 1.8f));
        AssetDatabase.SaveAssets();
    }

    static Material Existing(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat");

    static Material Copy(string newName, string sourceName, Color baseColor, Color? emission = null)
    {
        string path = MatFolder + "/" + newName + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var source = Existing(sourceName);
            mat = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", baseColor);
        mat.color = baseColor;
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
