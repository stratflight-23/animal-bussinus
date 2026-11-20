using UnityEngine;

public enum LootType { GoldenBanana, GoopJar, ShinyGem, OldCoinPile, CrystalSkull, LostToyRobot, TempleIdol }

// A piece of treasure from the Jungle Temple. Pick it up (grip), carry it back to the Sell Bin
// in the Treehouse Base and drop it in to get Goop Coins.
// Loot is built from simple shapes when it spawns (see LootSpawnPoint), so there are no prefab files.
[RequireComponent(typeof(Grabbable))]
public class LootItem : MonoBehaviour
{
    public LootType type;
    public string lootName;
    [Tooltip("How many Goop Coins the Sell Bin pays for this.")]
    public int coinValue;

    Grabbable grab;
    public bool IsHeld => grab != null && grab.IsHeld;
    public bool Sold { get; private set; }

    void Awake() => grab = GetComponent<Grabbable>();

    public void MarkSold() => Sold = true;

    // ------------------------------------------------------------ names and prices

    public static int ValueOf(LootType t)
    {
        switch (t)
        {
            case LootType.GoldenBanana: return 50;
            case LootType.GoopJar: return 20;
            case LootType.ShinyGem: return 75;
            case LootType.OldCoinPile: return 30;
            case LootType.CrystalSkull: return 120;
            case LootType.LostToyRobot: return 40;
            case LootType.TempleIdol: return 200;
        }
        return 10;
    }

    public static string NameOf(LootType t)
    {
        switch (t)
        {
            case LootType.GoldenBanana: return "Golden Banana";
            case LootType.GoopJar: return "Goop Jar";
            case LootType.ShinyGem: return "Shiny Gem";
            case LootType.OldCoinPile: return "Old Coin Pile";
            case LootType.CrystalSkull: return "Crystal Skull";
            case LootType.LostToyRobot: return "Lost Toy Robot";
            case LootType.TempleIdol: return "Temple Idol";
        }
        return t.ToString();
    }

    // ------------------------------------------------------------ building one

    static readonly Color Gold = new Color(1f, 0.78f, 0.15f);
    static readonly Color Bronze = new Color(0.85f, 0.6f, 0.25f);
    static readonly Color Stem = new Color(0.4f, 0.26f, 0.1f);

    // Makes a loot item sitting still (it only starts falling/rolling once someone grabs and lets go of it).
    public static LootItem Create(LootType type, Vector3 position, Quaternion rotation)
    {
        var root = new GameObject(NameOf(type));
        var v = new GameObject("Visual").transform;
        v.SetParent(root.transform, false);
        BuildLook(type, v);

        // collider that fits the shape
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }
        var box = root.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = Vector3.Max(bounds.size, Vector3.one * 0.06f);

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.4f;
        rb.isKinematic = true; // sits still until grabbed (cheap for Quest)
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        root.AddComponent<Grabbable>();
        var loot = root.AddComponent<LootItem>();
        loot.type = type;
        loot.lootName = NameOf(type);
        loot.coinValue = ValueOf(type);

        root.transform.SetPositionAndRotation(position, rotation);
        return loot;
    }

    // Origin = bottom center, so it sits on the floor. Sizes are in meters.
    static void BuildLook(LootType type, Transform v)
    {
        var S = PrimitiveType.Sphere;
        var C = PrimitiveType.Cube;
        var Y = PrimitiveType.Cylinder;
        var P = PrimitiveType.Capsule;

        switch (type)
        {
            case LootType.GoldenBanana:   // a curved banana from 3 bent capsules
                KittyLook.Part(v, P, "Banana", new Vector3(-0.065f, 0.06f, 0f), new Vector3(0.05f, 0.055f, 0.05f), new Vector3(0f, 0f, 55f), Gold);
                KittyLook.Part(v, P, "Banana", new Vector3(0f, 0.035f, 0f), new Vector3(0.055f, 0.055f, 0.055f), new Vector3(0f, 0f, 90f), Gold);
                KittyLook.Part(v, P, "Banana", new Vector3(0.065f, 0.06f, 0f), new Vector3(0.05f, 0.055f, 0.05f), new Vector3(0f, 0f, -55f), Gold);
                KittyLook.Part(v, Y, "Stem", new Vector3(-0.1f, 0.11f, 0f), new Vector3(0.018f, 0.015f, 0.018f), new Vector3(0f, 0f, 35f), Stem);
                KittyLook.Part(v, S, "Tip", new Vector3(0.1f, 0.095f, 0f), Vector3.one * 0.018f, Vector3.zero, Stem);
                break;

            case LootType.GoopJar:        // a jar of bubbling green goop
                KittyLook.Part(v, Y, "Jar", new Vector3(0f, 0.065f, 0f), new Vector3(0.1f, 0.065f, 0.1f), Vector3.zero, new Color(0.55f, 1f, 0.6f));
                var goop = KittyLook.Part(v, S, "Goop", new Vector3(0f, 0.13f, 0f), new Vector3(0.085f, 0.035f, 0.085f), Vector3.zero, FxSpawner.GoopGreen);
                KittyLook.MakeGlow(goop, FxSpawner.GoopGreen);
                KittyLook.Part(v, Y, "Lid", new Vector3(0f, 0.15f, 0f), new Vector3(0.11f, 0.012f, 0.11f), Vector3.zero, Stem);
                KittyLook.Part(v, C, "Label", new Vector3(0f, 0.06f, 0.05f), new Vector3(0.05f, 0.04f, 0.004f), Vector3.zero, Color.white);
                break;

            case LootType.ShinyGem:       // a diamond shape that glows
                var gem = KittyLook.Part(v, C, "Gem", new Vector3(0f, 0.08f, 0f), Vector3.one * 0.09f, new Vector3(45f, 0f, 45f), new Color(0.3f, 0.95f, 1f));
                KittyLook.MakeGlow(gem, new Color(0.3f, 0.95f, 1f));
                KittyLook.Part(v, C, "Sparkle", new Vector3(0.02f, 0.12f, 0.02f), Vector3.one * 0.02f, new Vector3(45f, 45f, 0f), Color.white);
                break;

            case LootType.OldCoinPile:    // a messy stack of old coins
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 2.4f;
                    var pos = new Vector3(Mathf.Cos(a) * 0.02f * (i % 3), 0.006f + i * 0.012f, Mathf.Sin(a) * 0.02f * (i % 3));
                    KittyLook.Part(v, Y, "Coin", pos, new Vector3(0.07f, 0.006f, 0.07f), new Vector3(i * 3f, 0f, i * 2f), i % 2 == 0 ? Gold : Bronze);
                }
                break;

            case LootType.CrystalSkull:   // a cute, round, see-through-blue skull
                Color crystal = new Color(0.65f, 0.92f, 1f);
                var head = KittyLook.Part(v, S, "Head", new Vector3(0f, 0.1f, 0f), new Vector3(0.14f, 0.13f, 0.15f), Vector3.zero, crystal);
                KittyLook.MakeGlow(head, crystal);
                KittyLook.Part(v, C, "Jaw", new Vector3(0f, 0.035f, 0.02f), new Vector3(0.09f, 0.04f, 0.08f), Vector3.zero, crystal);
                KittyLook.Part(v, S, "Eye", new Vector3(-0.032f, 0.105f, 0.062f), Vector3.one * 0.035f, Vector3.zero, new Color(0.25f, 0.1f, 0.4f));
                KittyLook.Part(v, S, "Eye", new Vector3(0.032f, 0.105f, 0.062f), Vector3.one * 0.035f, Vector3.zero, new Color(0.25f, 0.1f, 0.4f));
                break;

            case LootType.LostToyRobot:   // a little wind-up robot someone dropped long ago
                Color tin = new Color(0.45f, 0.6f, 0.85f);
                KittyLook.Part(v, C, "Leg", new Vector3(-0.025f, 0.02f, 0f), new Vector3(0.03f, 0.04f, 0.04f), Vector3.zero, tin * 0.8f);
                KittyLook.Part(v, C, "Leg", new Vector3(0.025f, 0.02f, 0f), new Vector3(0.03f, 0.04f, 0.04f), Vector3.zero, tin * 0.8f);
                KittyLook.Part(v, C, "Body", new Vector3(0f, 0.09f, 0f), new Vector3(0.09f, 0.1f, 0.07f), Vector3.zero, tin);
                KittyLook.Part(v, Y, "Arm", new Vector3(-0.058f, 0.09f, 0f), new Vector3(0.02f, 0.035f, 0.02f), Vector3.zero, tin * 0.8f);
                KittyLook.Part(v, Y, "Arm", new Vector3(0.058f, 0.09f, 0f), new Vector3(0.02f, 0.035f, 0.02f), Vector3.zero, tin * 0.8f);
                KittyLook.Part(v, C, "Head", new Vector3(0f, 0.17f, 0f), new Vector3(0.07f, 0.06f, 0.06f), Vector3.zero, tin);
                KittyLook.Part(v, C, "Eye", new Vector3(-0.017f, 0.175f, 0.031f), new Vector3(0.015f, 0.012f, 0.004f), Vector3.zero, new Color(1f, 0.9f, 0.2f));
                KittyLook.Part(v, C, "Eye", new Vector3(0.017f, 0.175f, 0.031f), new Vector3(0.015f, 0.012f, 0.004f), Vector3.zero, new Color(1f, 0.9f, 0.2f));
                KittyLook.Part(v, Y, "Antenna", new Vector3(0f, 0.215f, 0f), new Vector3(0.008f, 0.015f, 0.008f), Vector3.zero, Color.grey);
                KittyLook.Part(v, S, "Antenna Ball", new Vector3(0f, 0.235f, 0f), Vector3.one * 0.02f, Vector3.zero, new Color(1f, 0.25f, 0.25f));
                KittyLook.Part(v, Y, "Wind-up Key", new Vector3(0f, 0.09f, -0.045f), new Vector3(0.012f, 0.012f, 0.012f), new Vector3(90f, 0f, 0f), Gold);
                break;

            case LootType.TempleIdol:     // the big prize: a glowing golden statue with green gem eyes
                KittyLook.Part(v, C, "Base", new Vector3(0f, 0.02f, 0f), new Vector3(0.14f, 0.04f, 0.14f), Vector3.zero, new Color(0.3f, 0.3f, 0.28f));
                var body = KittyLook.Part(v, P, "Body", new Vector3(0f, 0.11f, 0f), new Vector3(0.09f, 0.065f, 0.08f), Vector3.zero, Gold);
                KittyLook.MakeGlow(body, Gold);
                var face = KittyLook.Part(v, S, "Head", new Vector3(0f, 0.2f, 0f), Vector3.one * 0.09f, Vector3.zero, Gold);
                KittyLook.MakeGlow(face, Gold);
                for (int i = -1; i <= 1; i++)
                    KittyLook.Part(v, C, "Crown", new Vector3(i * 0.025f, 0.25f, 0f), Vector3.one * 0.025f, new Vector3(0f, 0f, 45f), Gold);
                var eyeL = KittyLook.Part(v, S, "Gem Eye", new Vector3(-0.02f, 0.205f, 0.04f), Vector3.one * 0.02f, Vector3.zero, FxSpawner.GoopGreen);
                var eyeR = KittyLook.Part(v, S, "Gem Eye", new Vector3(0.02f, 0.205f, 0.04f), Vector3.one * 0.02f, Vector3.zero, FxSpawner.GoopGreen);
                KittyLook.MakeGlow(eyeL, FxSpawner.GoopGreen);
                KittyLook.MakeGlow(eyeR, FxSpawner.GoopGreen);
                var glow = new GameObject("Glow").AddComponent<Light>();
                glow.transform.SetParent(v, false);
                glow.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                glow.type = LightType.Point;
                glow.color = Gold;
                glow.range = 2.5f;
                glow.intensity = 1.5f;
                glow.shadows = LightShadows.None;
                break;
        }
    }
}
