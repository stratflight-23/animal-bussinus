using System.Collections.Generic;
using UnityEngine;

// The 30 original cosmetics, each built from simple shapes. Gorilla Company > 10. Generate Cosmetics turns these
// into files in Assets/Cosmetics/Data. To change a cosmetic, edit it here and run the menu again.
// Coordinates (meters): Head/Face = middle of the head (head is a ball 0.24 wide, +Z = front).
// Neck = under the chin. Back = middle of the body (+Z = chest side, so -Z is your back). Hands = middle of the hand.
public static class CosmeticCatalog
{
    public class Def
    {
        public string id, name, desc;
        public CosmeticSlot slot;
        public List<PartRecipe> parts = new List<PartRecipe>();
        public BodyColorStyle style;
        public Color bodyColor = Color.white;
    }

    static readonly Color Yellow = new Color(1f, 0.85f, 0.2f);
    static readonly Color Brown = new Color(0.42f, 0.27f, 0.13f);
    static readonly Color DarkBrown = new Color(0.25f, 0.15f, 0.07f);
    static readonly Color Leaf = new Color(0.25f, 0.7f, 0.25f);
    static readonly Color DarkLeaf = new Color(0.12f, 0.45f, 0.15f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.15f);
    static readonly Color Orange = new Color(1f, 0.5f, 0.1f);
    static readonly Color Red = new Color(0.9f, 0.15f, 0.15f);
    static readonly Color Pink = new Color(1f, 0.45f, 0.7f);
    static readonly Color White = new Color(0.97f, 0.97f, 0.97f);
    static readonly Color Black = new Color(0.08f, 0.08f, 0.1f);
    static readonly Color Blue = new Color(0.2f, 0.45f, 0.95f);
    static readonly Color Cyan = new Color(0.35f, 0.95f, 1f);
    static readonly Color Silver = new Color(0.78f, 0.8f, 0.85f);
    static readonly Color Goop = new Color(0.24f, 0.86f, 0.35f);

    // shape helpers: S = sphere, C = cube, Y = cylinder, P = capsule
    static PartRecipe S(float x, float y, float z, Vector3 size, Color c, Vector3 rot = default, bool glow = false) => new PartRecipe(PrimitiveType.Sphere, size, new Vector3(x, y, z), c, rot, null, glow);
    static PartRecipe C(float x, float y, float z, Vector3 size, Color c, Vector3 rot = default, bool glow = false, float spin = 0f) => new PartRecipe(PrimitiveType.Cube, size, new Vector3(x, y, z), c, rot, null, glow, spin);
    static PartRecipe Y(float x, float y, float z, Vector3 size, Color c, Vector3 rot = default, bool glow = false) => new PartRecipe(PrimitiveType.Cylinder, size, new Vector3(x, y, z), c, rot, null, glow);
    static PartRecipe P(float x, float y, float z, Vector3 size, Color c, Vector3 rot = default, bool glow = false) => new PartRecipe(PrimitiveType.Capsule, size, new Vector3(x, y, z), c, rot, null, glow);
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Vector3 U(float s) => Vector3.one * s;

    static Def Item(List<Def> list, string id, string name, CosmeticSlot slot, string desc, params PartRecipe[] parts)
    {
        var d = new Def { id = id, name = name, slot = slot, desc = desc };
        d.parts.AddRange(parts);
        list.Add(d);
        return d;
    }

    public static List<Def> All()
    {
        var L = new List<Def>();
        var H = CosmeticSlot.Head; var F = CosmeticSlot.Face; var N = CosmeticSlot.Neck; var B = CosmeticSlot.Back; var G = CosmeticSlot.Hands;

        // ---------------------------------------------------------------- HEAD
        Item(L, "banana_hat", "Banana Hat", H, "A whole banana, balanced on top. Very stylish.",
            P(0f, 0.15f, 0f, V(0.075f, 0.09f, 0.075f), Yellow, V(0, 0, 90)),
            P(-0.11f, 0.12f, 0f, V(0.065f, 0.065f, 0.065f), Yellow, V(0, 0, 60)),
            P(0.11f, 0.12f, 0f, V(0.065f, 0.065f, 0.065f), Yellow, V(0, 0, -60)),
            S(-0.155f, 0.075f, 0f, U(0.028f), DarkBrown), S(0.155f, 0.075f, 0f, U(0.028f), DarkBrown));

        var crown = Item(L, "leaf_crown", "Leaf Crown", H, "Six fresh jungle leaves in a ring.",
            Y(0f, 0.085f, 0f, V(0.23f, 0.012f, 0.23f), DarkLeaf));
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f;
            Vector3 p = Quaternion.Euler(0, a, 0) * V(0f, 0.12f, 0.1f);
            crown.parts.Add(S(p.x, p.y, p.z, V(0.05f, 0.1f, 0.012f), i % 2 == 0 ? Leaf : DarkLeaf, V(-20f, a, 0f)));
        }

        Item(L, "coconut_helmet", "Coconut Helmet", H, "Half a coconut. Safety first!",
            S(0f, 0.045f, 0f, V(0.275f, 0.21f, 0.275f), Brown),
            S(-0.04f, 0.13f, 0.07f, U(0.03f), DarkBrown), S(0.04f, 0.13f, 0.07f, U(0.03f), DarkBrown), S(0f, 0.16f, 0.04f, U(0.03f), DarkBrown),
            Y(0f, 0.045f, 0f, V(0.28f, 0.006f, 0.28f), White));

        Item(L, "party_cone", "Party Cone", H, "It's always a party in the jungle.",
            Y(0f, 0.135f, 0f, V(0.13f, 0.025f, 0.13f), Pink), Y(0f, 0.18f, 0f, V(0.1f, 0.025f, 0.1f), Yellow),
            Y(0f, 0.225f, 0f, V(0.07f, 0.025f, 0.07f), Pink), Y(0f, 0.265f, 0f, V(0.04f, 0.02f, 0.04f), Yellow),
            S(0f, 0.3f, 0f, U(0.045f), White));

        Item(L, "traffic_cone", "Traffic Cone", H, "Please drive around this gorilla.",
            C(0f, 0.115f, 0f, V(0.2f, 0.02f, 0.2f), Orange),
            Y(0f, 0.16f, 0f, V(0.14f, 0.035f, 0.14f), Orange), Y(0f, 0.21f, 0f, V(0.105f, 0.018f, 0.105f), White),
            Y(0f, 0.25f, 0f, V(0.08f, 0.025f, 0.08f), Orange), Y(0f, 0.29f, 0f, V(0.045f, 0.018f, 0.045f), Orange));

        Item(L, "propeller_cap", "Propeller Cap", H, "The propeller really spins. (It doesn't make you fly. Yet.)",
            S(0f, 0.07f, 0f, V(0.255f, 0.15f, 0.255f), Blue), C(0f, 0.06f, 0.15f, V(0.15f, 0.012f, 0.1f), Red),
            Y(0f, 0.155f, 0f, V(0.015f, 0.02f, 0.015f), Silver),
            C(0f, 0.175f, 0f, V(0.24f, 0.006f, 0.035f), Yellow, default, false, 720f),
            C(0f, 0.176f, 0f, V(0.035f, 0.006f, 0.24f), Red, default, false, 720f));

        Item(L, "crystal_tiara", "Crystal Tiara", H, "Three glowing temple crystals on a silver band.",
            Y(0f, 0.09f, 0f, V(0.235f, 0.01f, 0.235f), Silver),
            C(0f, 0.135f, 0.085f, V(0.03f, 0.075f, 0.03f), Cyan, V(0, 45, 0), true),
            C(-0.06f, 0.12f, 0.07f, V(0.025f, 0.05f, 0.025f), Cyan, V(0, 45, 15), true),
            C(0.06f, 0.12f, 0.07f, V(0.025f, 0.05f, 0.025f), Cyan, V(0, 45, -15), true));

        Item(L, "pirate_bandana", "Pirate Bandana", H, "Arr! A red bandana with white spots.",
            S(0f, 0.045f, -0.005f, V(0.256f, 0.17f, 0.256f), Red),
            S(-0.05f, 0.11f, 0.07f, U(0.025f), White), S(0.04f, 0.12f, 0.06f, U(0.025f), White), S(0.07f, 0.07f, -0.06f, U(0.025f), White),
            S(0f, 0.03f, -0.13f, U(0.04f), Red),
            C(0.025f, -0.02f, -0.14f, V(0.03f, 0.08f, 0.01f), Red, V(0, 0, 20)), C(-0.025f, -0.02f, -0.14f, V(0.03f, 0.08f, 0.01f), Red, V(0, 0, -20)));

        Item(L, "chef_hat", "Chef Hat", H, "For gorillas who cook banana bread.",
            Y(0f, 0.11f, 0f, V(0.2f, 0.035f, 0.2f), White),
            S(0f, 0.2f, 0f, U(0.17f), White), S(-0.06f, 0.18f, 0f, U(0.12f), White), S(0.06f, 0.18f, 0f, U(0.12f), White),
            S(0f, 0.18f, 0.06f, U(0.12f), White));

        Item(L, "goop_bucket", "Goop Bucket", H, "An upside-down bucket, still dripping with goop.",
            Y(0f, 0.15f, 0f, V(0.25f, 0.09f, 0.25f), new Color(0.55f, 0.6f, 0.65f)),
            Y(0f, 0.061f, 0f, V(0.26f, 0.008f, 0.26f), Goop, default, true),
            S(0.1f, 0.05f, 0.06f, U(0.035f), Goop, default, true), S(-0.08f, 0.045f, -0.08f, U(0.03f), Goop, default, true),
            S(0.02f, 0.04f, 0.12f, U(0.03f), Goop, default, true),
            C(0f, 0.25f, 0f, V(0.2f, 0.01f, 0.012f), Silver));

        // ---------------------------------------------------------------- FACE
        Item(L, "big_sunglasses", "Big Sunglasses", F, "Too cool for the jungle.",
            Y(-0.048f, 0.02f, 0.13f, V(0.085f, 0.005f, 0.07f), Black, V(90, 0, 0)),
            Y(0.048f, 0.02f, 0.13f, V(0.085f, 0.005f, 0.07f), Black, V(90, 0, 0)),
            C(0f, 0.025f, 0.13f, V(0.03f, 0.008f, 0.008f), Black),
            C(-0.105f, 0.025f, 0.07f, V(0.008f, 0.008f, 0.12f), Black), C(0.105f, 0.025f, 0.07f, V(0.008f, 0.008f, 0.12f), Black));

        Item(L, "star_glasses", "Star Glasses", F, "Pink star-shaped party glasses.",
            C(-0.05f, 0.02f, 0.13f, V(0.065f, 0.065f, 0.008f), Pink), C(-0.05f, 0.02f, 0.13f, V(0.065f, 0.065f, 0.008f), Pink, V(0, 0, 45)),
            C(0.05f, 0.02f, 0.13f, V(0.065f, 0.065f, 0.008f), Pink), C(0.05f, 0.02f, 0.13f, V(0.065f, 0.065f, 0.008f), Pink, V(0, 0, 45)),
            C(0f, 0.02f, 0.13f, V(0.03f, 0.008f, 0.008f), Yellow));

        Item(L, "snorkel_mask", "Snorkel Mask", F, "Ready for a swim in the river.",
            S(0f, 0.025f, 0.12f, V(0.19f, 0.1f, 0.06f), new Color(0.55f, 0.85f, 1f)),
            Y(0f, 0.03f, 0f, V(0.255f, 0.015f, 0.255f), Black),
            Y(0.11f, 0.1f, 0.05f, V(0.025f, 0.09f, 0.025f), Yellow), Y(0.11f, 0.19f, 0.05f, V(0.03f, 0.012f, 0.03f), Orange));

        Item(L, "mustache", "Mustache", F, "A fancy curly mustache.",
            P(-0.035f, -0.045f, 0.125f, V(0.03f, 0.04f, 0.025f), DarkBrown, V(0, 0, 70)),
            P(0.035f, -0.045f, 0.125f, V(0.03f, 0.04f, 0.025f), DarkBrown, V(0, 0, -70)),
            S(-0.075f, -0.035f, 0.115f, U(0.022f), DarkBrown), S(0.075f, -0.035f, 0.115f, U(0.022f), DarkBrown));

        Item(L, "clown_nose", "Clown Nose", F, "Honk honk.",
            S(0f, -0.015f, 0.135f, U(0.05f), Red));

        Item(L, "ski_goggles", "Ski Goggles", F, "Shiny orange goggles for snowy mountains (we'll add some).",
            C(0f, 0.03f, 0.12f, V(0.2f, 0.065f, 0.03f), White),
            C(0f, 0.03f, 0.137f, V(0.18f, 0.05f, 0.005f), Orange, default, true),
            Y(0f, 0.03f, 0f, V(0.255f, 0.02f, 0.255f), Black));

        // ---------------------------------------------------------------- NECK
        Item(L, "bow_tie", "Bow Tie", N, "Dressed up for a fancy banana dinner.",
            C(-0.035f, 0f, 0.1f, V(0.05f, 0.05f, 0.02f), Red, V(0, 0, 45)), C(0.035f, 0f, 0.1f, V(0.05f, 0.05f, 0.02f), Red, V(0, 0, 45)),
            C(0f, 0f, 0.108f, V(0.025f, 0.025f, 0.02f), new Color(0.6f, 0.05f, 0.05f)));

        var chain = Item(L, "gold_chain", "Gold Chain", N, "Big, shiny and very heavy.",
            Y(0f, -0.075f, 0.11f, V(0.06f, 0.006f, 0.06f), Gold, V(90, 0, 0), true));
        for (int i = 0; i < 14; i++)
        {
            Vector3 p = Quaternion.Euler(0, i * 360f / 14f, 0) * V(0f, 0f, 0.105f);
            chain.parts.Add(S(p.x, p.y - 0.02f + (Mathf.Cos(i * Mathf.PI * 2f / 14f) > 0.5f ? -0.02f : 0f), p.z, U(0.022f), Gold));
        }

        var flowers = Item(L, "flower_necklace", "Flower Necklace", N, "A ring of jungle flowers.");
        for (int i = 0; i < 10; i++)
        {
            Vector3 p = Quaternion.Euler(0, i * 36f, 0) * V(0f, -0.02f, 0.11f);
            Color c = i % 3 == 0 ? Pink : i % 3 == 1 ? Yellow : White;
            flowers.parts.Add(S(p.x, p.y, p.z, V(0.04f, 0.04f, 0.02f), c, V(0, i * 36f, 0)));
            flowers.parts.Add(S(p.x, p.y, p.z, U(0.015f), Orange));
        }

        Item(L, "scarf", "Scarf", N, "A cozy striped scarf.",
            Y(0f, -0.01f, 0f, V(0.24f, 0.03f, 0.24f), Red), Y(0f, 0.015f, 0f, V(0.22f, 0.012f, 0.22f), White),
            C(0.05f, -0.1f, 0.11f, V(0.055f, 0.16f, 0.02f), Red, V(0, 0, 8)), C(0.05f, -0.14f, 0.112f, V(0.056f, 0.025f, 0.02f), White, V(0, 0, 8)));

        // ---------------------------------------------------------------- BACK
        Item(L, "banana_backpack", "Banana Backpack", B, "A yellow backpack stuffed with bananas.",
            C(0f, 0.04f, -0.21f, V(0.24f, 0.28f, 0.12f), Yellow), C(0f, 0.18f, -0.21f, V(0.25f, 0.03f, 0.13f), Brown),
            P(-0.05f, 0.24f, -0.21f, V(0.04f, 0.06f, 0.04f), Yellow, V(0, 0, 15)), P(0.04f, 0.25f, -0.2f, V(0.04f, 0.06f, 0.04f), Yellow, V(0, 0, -20)),
            C(-0.11f, 0.12f, -0.08f, V(0.03f, 0.03f, 0.26f), Brown), C(0.11f, 0.12f, -0.08f, V(0.03f, 0.03f, 0.26f), Brown));

        Item(L, "mini_jetpack", "Mini Jetpack", B, "Looks only... for now. Little flames glow out the bottom.",
            Y(-0.065f, 0.05f, -0.22f, V(0.09f, 0.13f, 0.09f), Silver), Y(0.065f, 0.05f, -0.22f, V(0.09f, 0.13f, 0.09f), Silver),
            S(-0.065f, 0.18f, -0.22f, U(0.09f), Red), S(0.065f, 0.18f, -0.22f, U(0.09f), Red),
            Y(-0.065f, -0.09f, -0.22f, V(0.06f, 0.02f, 0.06f), Black), Y(0.065f, -0.09f, -0.22f, V(0.06f, 0.02f, 0.06f), Black),
            S(-0.065f, -0.13f, -0.22f, V(0.05f, 0.08f, 0.05f), Orange, default, true), S(0.065f, -0.13f, -0.22f, V(0.05f, 0.08f, 0.05f), Orange, default, true));

        Item(L, "superhero_cape", "Superhero Cape", B, "A red cape that says you're here to help.",
            C(0f, -0.13f, -0.2f, V(0.4f, 0.6f, 0.015f), Red, V(8, 0, 0)), C(0f, 0.17f, -0.16f, V(0.4f, 0.04f, 0.06f), Gold));

        Item(L, "leaf_wings", "Leaf Wings", B, "Two giant leaves. Flap flap!",
            S(-0.2f, 0.12f, -0.2f, V(0.36f, 0.16f, 0.012f), Leaf, V(0, -25, 25)), S(0.2f, 0.12f, -0.2f, V(0.36f, 0.16f, 0.012f), Leaf, V(0, 25, -25)),
            C(-0.2f, 0.12f, -0.205f, V(0.3f, 0.008f, 0.01f), DarkLeaf, V(0, -25, 25)), C(0.2f, 0.12f, -0.205f, V(0.3f, 0.008f, 0.01f), DarkLeaf, V(0, 25, -25)));

        Item(L, "turtle_shell", "Turtle Shell", B, "Slow and steady. A green shell with patches.",
            S(0f, 0f, -0.2f, V(0.4f, 0.45f, 0.2f), new Color(0.2f, 0.55f, 0.25f)),
            S(0f, 0.06f, -0.29f, V(0.12f, 0.12f, 0.03f), new Color(0.35f, 0.7f, 0.3f)),
            S(-0.1f, -0.06f, -0.27f, V(0.1f, 0.1f, 0.03f), new Color(0.35f, 0.7f, 0.3f)),
            S(0.1f, -0.06f, -0.27f, V(0.1f, 0.1f, 0.03f), new Color(0.35f, 0.7f, 0.3f)),
            Y(0f, 0f, -0.18f, V(0.42f, 0.01f, 0.42f), new Color(0.85f, 0.75f, 0.4f), V(90, 0, 0)));

        // ---------------------------------------------------------------- HANDS
        Item(L, "boxing_gloves", "Boxing Gloves", G, "Big red puffy gloves.",
            S(0f, 0f, 0.015f, V(0.12f, 0.11f, 0.14f), Red), Y(0f, 0f, -0.06f, V(0.09f, 0.025f, 0.09f), White, V(90, 0, 0)));

        Item(L, "goop_gloves", "Goop Gloves", G, "Hands covered in glowing green goop.",
            S(0f, 0f, 0f, U(0.115f), Goop, default, true),
            S(0.03f, -0.055f, 0.02f, U(0.03f), Goop, default, true), S(-0.035f, -0.05f, -0.02f, U(0.025f), Goop, default, true));

        Item(L, "glow_hands", "Glow Hands", G, "Your hands glow bright blue.",
            S(0f, 0f, 0f, U(0.112f), Cyan, default, true));

        // ---------------------------------------------------------------- BODY COLOR
        var rainbow = Item(L, "body_rainbow", "Rainbow", CosmeticSlot.BodyColor, "Your fur slowly changes through every color.");
        rainbow.style = BodyColorStyle.Rainbow;
        rainbow.bodyColor = Color.red;
        var glow = Item(L, "body_glow_green", "Glow-in-the-Dark Green", CosmeticSlot.BodyColor, "Bright green fur that glows in dark temples.");
        glow.style = BodyColorStyle.Glow;
        glow.bodyColor = new Color(0.35f, 1f, 0.4f);
        return L;
    }
}
