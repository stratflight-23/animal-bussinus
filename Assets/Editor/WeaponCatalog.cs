using System;
using System.Collections.Generic;
using UnityEngine;

// All 100 Gorilla Company weapons: names, stats and looks (made only from cubes, spheres, capsules, cylinders).
// The generator (Gorilla Company > 4. Generate All Weapons) turns these into WeaponData assets + prefabs.
// Coordinates: meters, measured from where your hand holds it. +Z points away from your hand.
public static class WeaponCatalog
{
    public class Def
    {
        public string name, category, description;
        public AttackType attack;
        public WeaponEffect effect;
        public Color main, accent;
        public Func<Color, Color, Recipe> look;
        public Action<WeaponData> tweak;
    }

    // ------------------------------------------------------------ shape recipe helper

    public class Recipe
    {
        public readonly List<ShapePart> parts = new List<ShapePart>();

        public Recipe Add(PrimitiveType t, Vector3 scale, Vector3 pos, Color c, Vector3 rot = default)
        {
            parts.Add(new ShapePart { shape = t, size = scale, offset = pos, rotation = rot, color = c });
            return this;
        }

        // cylinder lying along Z, from z0 to z1
        public Recipe Rod(float z0, float z1, float diam, Color c, float x = 0f, float y = 0f) =>
            Add(PrimitiveType.Cylinder, new Vector3(diam, (z1 - z0) / 2f, diam), new Vector3(x, y, (z0 + z1) / 2f), c, new Vector3(90, 0, 0));
        // standing cylinder, from y0 to y1
        public Recipe Post(float y0, float y1, float diam, Color c, float x = 0f, float z = 0f) =>
            Add(PrimitiveType.Cylinder, new Vector3(diam, (y1 - y0) / 2f, diam), new Vector3(x, (y0 + y1) / 2f, z), c);
        // cylinder lying along X
        public Recipe Bar(float x0, float x1, float diam, Color c, float y = 0f, float z = 0f) =>
            Add(PrimitiveType.Cylinder, new Vector3(diam, (x1 - x0) / 2f, diam), new Vector3((x0 + x1) / 2f, y, z), c, new Vector3(0, 0, 90));
        // capsule lying along Z
        public Recipe Pill(float z0, float z1, float diam, Color c, float x = 0f, float y = 0f) =>
            Add(PrimitiveType.Capsule, new Vector3(diam, (z1 - z0) / 2f, diam), new Vector3(x, y, (z0 + z1) / 2f), c, new Vector3(90, 0, 0));
        public Recipe Ball(float d, float x, float y, float z, Color c) =>
            Add(PrimitiveType.Sphere, Vector3.one * d, new Vector3(x, y, z), c);
        public Recipe Egg(float sx, float sy, float sz, float x, float y, float z, Color c, float rx = 0, float ry = 0, float rz = 0) =>
            Add(PrimitiveType.Sphere, new Vector3(sx, sy, sz), new Vector3(x, y, z), c, new Vector3(rx, ry, rz));
        public Recipe Box(float sx, float sy, float sz, float x, float y, float z, Color c, float rx = 0, float ry = 0, float rz = 0) =>
            Add(PrimitiveType.Cube, new Vector3(sx, sy, sz), new Vector3(x, y, z), c, new Vector3(rx, ry, rz));
        // flat disc facing along Z (like a coin you look at from the front)
        public Recipe DiscZ(float diam, float thick, float z, Color c, float x = 0f, float y = 0f) =>
            Add(PrimitiveType.Cylinder, new Vector3(diam, thick / 2f, diam), new Vector3(x, y, z), c, new Vector3(90, 0, 0));
        // flat disc lying down (facing up)
        public Recipe DiscY(float diam, float thick, float x, float y, float z, Color c) =>
            Add(PrimitiveType.Cylinder, new Vector3(diam, thick / 2f, diam), new Vector3(x, y, z), c);
        public Recipe Handle(Color c, float len = 0.28f, float diam = 0.045f) => Rod(-len / 2f, len / 2f, diam, c);
        // pistol grip for blasters
        public Recipe Grip(Color c) => Box(0.045f, 0.14f, 0.06f, 0f, -0.04f, 0f, c, 15f);
    }

    static Color H(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;

    static readonly Color Wood = H("8B5A2B"), DarkWood = H("5C3A1E"), Goop = H("3DDC5A"), GoopDark = H("1F8A3A"),
        Banana = H("FFD93B"), Coconut = H("6B4226"), Bamboo = H("8FC93A"), BambooRing = H("5E8F22"),
        Stone = H("8C8C8C"), Metal = H("B8C0C8"), Iron = H("4A4F55"), Red = H("E8413C"), Pink = H("FF7FC8"),
        Ice = H("9FE8FF"), Leaf = H("4CAF50"), Orange = H("FF8C1A"), Purple = H("9B5DE5"), White = H("F5F5F5"),
        Black = H("222222"), Gold = H("F4C430"), Blue = H("3A86FF"), Cyan = H("34D1E0"), Straw = H("E8C872"),
        Cream = H("FFF1D0"), Tan = H("D9A86C"), Teal = H("1FA5A0"), Yellow = H("FFE14D");

    static readonly List<Def> all = new List<Def>();
    static string currentCategory;

    static void Cat(string name) => currentCategory = name;

    static void W(string name, AttackType attack, WeaponEffect effect, Color main, Color accent, string description,
                  Func<Color, Color, Recipe> look, Action<WeaponData> tweak = null)
    {
        all.Add(new Def
        {
            name = name, category = currentCategory, attack = attack, effect = effect, main = main, accent = accent,
            description = description, look = look, tweak = tweak
        });
    }

    public static List<Def> All()
    {
        if (all.Count == 0) Build();
        return all;
    }

    // ------------------------------------------------------------ stats

    // Sensible starting numbers for each attack type and category. Each weapon can then tweak its own.
    public static void ApplyBaseStats(WeaponData d, Def def, int indexInCategory)
    {
        switch (def.attack)
        {
            case AttackType.Swing: Set(d, dmg: 12, cd: 0.4f, range: 0.18f, kb: 3f, mass: 1.2f); break;
            case AttackType.Stab: Set(d, dmg: 16, cd: 0.55f, range: 0.14f, kb: 3f, mass: 1.2f); break;
            case AttackType.Throw: Set(d, dmg: 18, cd: 0.5f, range: 0f, kb: 4f, mass: 0.5f); break;
            case AttackType.Shoot: Set(d, dmg: 7, cd: 0.3f, range: 30f, kb: 1.5f, mass: 1f); d.projectileSpeed = 22f; d.projectileSize = 0.08f; break;
            case AttackType.Trap: Set(d, dmg: 15, cd: 1f, range: 1.3f, kb: 5f, mass: 1.2f); d.singleUse = true; break;
            case AttackType.Use: Set(d, dmg: 0, cd: 5f, range: 0f, kb: 0f, mass: 0.6f); break;
        }

        switch (def.category)
        {
            case "Bonkers": d.knockback *= 1.4f; d.range = 0.22f; d.mass = 1.6f; break;
            case "Blades": d.cooldown *= 0.85f; break;
            case "Heavy Hitters":
                d.damage *= 2.3f; d.cooldown *= 2.5f; d.knockback *= 2.5f; d.projectileSpeed *= 0.6f;
                d.projectileSize *= 3f; d.mass = 4f;
                break;
            case "Elemental": d.damage *= 1.2f; if (def.attack == AttackType.Shoot) d.cooldown *= 2f; break;
            case "Silly": d.damage *= 0.4f; break;
        }

        // small differences inside a category so they don't all feel the same
        d.damage = Mathf.Round(d.damage * (0.9f + 0.025f * indexInCategory));
    }

    static void Set(WeaponData d, float dmg, float cd, float range, float kb, float mass)
    {
        d.damage = dmg; d.cooldown = cd; d.range = range; d.knockback = kb; d.mass = mass;
    }

    public static int PriceFor(WeaponData d)
    {
        float p = 40f + d.damage * 3f + Mathf.Abs(d.knockback) * 4f + (d.effect != WeaponEffect.None ? 35f : 0f)
                  + d.areaRadius * 20f - (d.singleUse ? 15f : 0f);
        if (d.category == "Heavy Hitters") p += 60f;
        if (d.category == "Support") p += 20f;
        return Mathf.Clamp(Mathf.RoundToInt(p / 5f) * 5, 25, 500);
    }

    // ------------------------------------------------------------ the 100 weapons

    static void Build()
    {
        var S = AttackType.Swing; var St = AttackType.Stab; var T = AttackType.Throw;
        var Sh = AttackType.Shoot; var Tr = AttackType.Trap; var U = AttackType.Use;
        var None = WeaponEffect.None;

        // ================= 1. BONKERS =================
        Cat("Bonkers");
        W("Goop Club", S, None, Goop, GoopDark, "A wobbly club of hardened goop. Bonk!",
            (m, a) => new Recipe().Handle(a).Pill(0.12f, 0.55f, 0.14f, m).Ball(0.18f, 0, 0, 0.52f, m).Ball(0.06f, 0.07f, 0.05f, 0.4f, a));
        W("Banana Bonker", S, WeaponEffect.Bounce, Banana, Coconut, "A giant banana. Bonked targets bounce away.",
            (m, a) => new Recipe().Handle(Tan, 0.24f)
                .Add(PrimitiveType.Capsule, new Vector3(0.11f, 0.2f, 0.11f), new Vector3(0, 0.02f, 0.3f), m, new Vector3(80, 0, 0))
                .Add(PrimitiveType.Capsule, new Vector3(0.1f, 0.18f, 0.1f), new Vector3(0, 0.09f, 0.56f), m, new Vector3(65, 0, 0))
                .Ball(0.05f, 0, 0.15f, 0.72f, a));
        W("Coconut Mace", S, None, Coconut, Straw, "A spiky coconut on a stick.",
            (m, a) => new Recipe().Rod(-0.15f, 0.25f, 0.045f, Wood).Ball(0.22f, 0, 0, 0.32f, m)
                .Box(0.06f, 0.06f, 0.06f, 0.12f, 0, 0.32f, a, 45, 45).Box(0.06f, 0.06f, 0.06f, -0.12f, 0, 0.32f, a, 45, 45)
                .Box(0.06f, 0.06f, 0.06f, 0, 0.12f, 0.32f, a, 45, 45).Box(0.06f, 0.06f, 0.06f, 0, -0.12f, 0.32f, a, 45, 45)
                .Box(0.06f, 0.06f, 0.06f, 0, 0, 0.44f, a, 45, 45));
        W("Bamboo Bat", S, None, Bamboo, BambooRing, "Light, fast and very green.",
            (m, a) => new Recipe().Rod(-0.15f, 0.15f, 0.05f, a).Rod(0.15f, 0.78f, 0.1f, m)
                .DiscZ(0.11f, 0.02f, 0.32f, a).DiscZ(0.11f, 0.02f, 0.48f, a).DiscZ(0.11f, 0.02f, 0.64f, a));
        W("Log Smacker", S, None, Wood, Tan, "A whole log. Heavy and slow, hits like a truck.",
            (m, a) => new Recipe().Handle(DarkWood).Rod(0.14f, 0.72f, 0.2f, m).DiscZ(0.17f, 0.02f, 0.725f, a)
                .Box(0.05f, 0.05f, 0.08f, 0.1f, 0.02f, 0.45f, DarkWood),
            d => { d.damage *= 1.25f; d.knockback *= 1.3f; d.cooldown = 0.7f; d.mass = 2.4f; });
        W("Pineapple Hammer", S, None, Yellow, Leaf, "Spiky fruit hammer. Smells great.",
            (m, a) => new Recipe().Rod(-0.15f, 0.5f, 0.045f, Wood).Egg(0.18f, 0.3f, 0.18f, 0, 0, 0.56f, m)
                .Box(0.03f, 0.14f, 0.06f, 0, 0.2f, 0.56f, a, 0, 0, 20).Box(0.03f, 0.14f, 0.06f, 0, 0.2f, 0.56f, a, 0, 0, -20)
                .Box(0.06f, 0.12f, 0.03f, 0, 0.19f, 0.56f, a));
        W("Rock-on-a-Stick", S, None, Stone, Leaf, "The oldest weapon on the island.",
            (m, a) => new Recipe().Rod(-0.15f, 0.45f, 0.04f, Wood).Egg(0.22f, 0.17f, 0.2f, 0, 0.02f, 0.5f, m, 10, 20)
                .DiscZ(0.06f, 0.05f, 0.4f, a));
        W("Drumstick Thumper", S, None, Red, Cream, "Plays a beat on whoever you hit.",
            (m, a) => new Recipe().Rod(-0.15f, 0.3f, 0.05f, a).Rod(0.3f, 0.56f, 0.04f, a).Ball(0.1f, 0, 0, 0.58f, m));
        W("Frying Pan", S, None, Iron, Black, "CLANG! Classic kitchen bonk.",
            (m, a) => new Recipe().Rod(-0.15f, 0.2f, 0.04f, a)
                .Add(PrimitiveType.Cylinder, new Vector3(0.34f, 0.02f, 0.34f), new Vector3(0, 0, 0.37f), m, new Vector3(0, 0, 90))
                .Add(PrimitiveType.Cylinder, new Vector3(0.28f, 0.025f, 0.28f), new Vector3(0, 0, 0.37f), H("6D737A"), new Vector3(0, 0, 90)),
            d => { d.knockback = 6f; });
        W("Pool Noodle Whacker", S, WeaponEffect.Bounce, Pink, Blue, "Barely hurts. Sends them flying though.",
            (m, a) => new Recipe().Rod(-0.15f, 0.15f, 0.045f, a).Rod(0.15f, 1.2f, 0.09f, m).DiscZ(0.08f, 0.01f, 1.205f, H("D04F98")),
            d => { d.damage = 4f; d.knockback = 8f; d.mass = 0.4f; });

        // ================= 2. BLADES =================
        Cat("Blades");
        W("Leaf Blade", S, None, Leaf, Wood, "A super-sharp jungle leaf.",
            (m, a) => new Recipe().Handle(a).Box(0.16f, 0.03f, 0.04f, 0, 0, 0.15f, DarkWood)
                .Egg(0.12f, 0.015f, 0.62f, 0, 0, 0.48f, m).Box(0.008f, 0.02f, 0.5f, 0, 0, 0.46f, H("2E7D32")));
        W("Bamboo Sword", S, None, Bamboo, BambooRing, "A training sword that's not just for training.",
            (m, a) => new Recipe().Handle(a).DiscZ(0.12f, 0.03f, 0.15f, a).Rod(0.16f, 0.85f, 0.05f, m)
                .DiscZ(0.06f, 0.02f, 0.4f, a).DiscZ(0.06f, 0.02f, 0.65f, a).Box(0.035f, 0.035f, 0.08f, 0, 0, 0.86f, m, 0, 0, 45));
        W("Vine Whip", S, WeaponEffect.Sticky, Leaf, Wood, "Wraps around targets and slows them down.",
            (m, a) =>
            {
                var r = new Recipe().Handle(a);
                for (int i = 0; i < 7; i++)
                    r.Pill(0.13f + i * 0.13f, 0.28f + i * 0.13f, 0.035f, m, 0, -i * i * 0.006f);
                return r.Box(0.06f, 0.01f, 0.04f, 0.03f, -0.02f, 0.45f, H("8BC34A"), 0, 30).Box(0.06f, 0.01f, 0.04f, -0.03f, -0.1f, 0.75f, H("8BC34A"), 0, -30);
            },
            d => { d.range = 0.2f; });
        W("Crystal Dagger", S, None, Cyan, Purple, "Tiny, shiny and very quick.",
            (m, a) => new Recipe().Handle(a, 0.2f).Box(0.12f, 0.03f, 0.03f, 0, 0, 0.11f, Gold)
                .Box(0.06f, 0.02f, 0.25f, 0, 0, 0.25f, m).Box(0.042f, 0.02f, 0.042f, 0, 0, 0.38f, m, 0, 45),
            d => { d.cooldown = 0.25f; d.mass = 0.5f; });
        W("Shell Sickle", S, None, Pink, Wood, "A curvy seashell blade.",
            (m, a) => new Recipe().Handle(a).Box(0.04f, 0.015f, 0.2f, 0, 0, 0.22f, m)
                .Box(0.04f, 0.015f, 0.18f, 0.06f, 0, 0.37f, m, 0, 40).Box(0.04f, 0.015f, 0.14f, 0.15f, 0, 0.44f, m, 0, 80)
                .Ball(0.03f, 0, 0.01f, 0.25f, White));
        W("Palm Frond Saber", S, None, Leaf, Coconut, "A whole palm frond. Swish swish.",
            (m, a) =>
            {
                var r = new Recipe().Handle(a).Rod(0.14f, 1.0f, 0.025f, H("C5B358"));
                for (int i = 0; i < 6; i++)
                {
                    float z = 0.25f + i * 0.13f;
                    r.Box(0.2f - i * 0.015f, 0.01f, 0.05f, 0.08f, 0, z, m, 0, -30).Box(0.2f - i * 0.015f, 0.01f, 0.05f, -0.08f, 0, z, m, 0, 30);
                }
                return r;
            });
        W("Flint Knife", S, None, Stone, Wood, "Chipped from a river rock.",
            (m, a) => new Recipe().Handle(a, 0.2f).DiscZ(0.055f, 0.03f, 0.06f, Straw).Egg(0.07f, 0.02f, 0.3f, 0, 0, 0.25f, m),
            d => { d.cooldown = 0.28f; d.mass = 0.5f; });
        W("Goop Katana", S, WeaponEffect.Sticky, Goop, Black, "A long, gooey blade that leaves targets sticky.",
            (m, a) => new Recipe().Handle(a, 0.3f).DiscZ(0.1f, 0.02f, 0.16f, Gold)
                .Box(0.035f, 0.012f, 0.45f, 0, 0, 0.4f, m).Box(0.035f, 0.012f, 0.38f, 0, 0.012f, 0.8f, m, -3));
        W("Jungle Machete", S, None, Metal, DarkWood, "Chops through vines and bad guys.",
            (m, a) => new Recipe().Handle(a).Box(0.11f, 0.012f, 0.48f, 0.02f, 0, 0.38f, m).Box(0.08f, 0.012f, 0.1f, 0.035f, 0, 0.64f, m, 0, 20));
        W("Ice Shard Sword", S, WeaponEffect.Freeze, Ice, Blue, "Made of never-melting ice. Freezes on hit.",
            (m, a) => new Recipe().Handle(a).Box(0.2f, 0.04f, 0.04f, 0, 0, 0.15f, White)
                .Box(0.07f, 0.07f, 0.6f, 0, 0, 0.47f, m, 0, 0, 45).Box(0.05f, 0.05f, 0.05f, 0, 0, 0.78f, m, 45, 45));

        // ================= 3. POKERS =================
        Cat("Pokers");
        W("Bamboo Spear", St, None, Bamboo, Stone, "Long reach. Poke from a safe distance.",
            (m, a) => new Recipe().Rod(-0.45f, 1.05f, 0.045f, m).DiscZ(0.055f, 0.02f, 0.3f, BambooRing).DiscZ(0.055f, 0.02f, 0.7f, BambooRing)
                .Box(0.07f, 0.07f, 0.16f, 0, 0, 1.12f, a, 0, 0, 45).Box(0.04f, 0.04f, 0.06f, 0, 0, 1.22f, a, 0, 0, 45));
        W("Tide Trident", St, WeaponEffect.Bounce, Teal, Gold, "Hits like a wave and washes targets away.",
            (m, a) => new Recipe().Rod(-0.45f, 0.95f, 0.04f, m).Box(0.28f, 0.03f, 0.03f, 0, 0, 0.97f, a)
                .Rod(0.97f, 1.2f, 0.025f, a, -0.13f).Rod(0.97f, 1.26f, 0.025f, a).Rod(0.97f, 1.2f, 0.025f, a, 0.13f));
        W("Goop Lance", St, WeaponEffect.Sticky, Goop, Gold, "A knightly lance dipped in goop.",
            (m, a) => new Recipe().Rod(-0.4f, 0.2f, 0.05f, Wood).DiscZ(0.2f, 0.03f, 0.2f, a)
                .Rod(0.2f, 0.5f, 0.16f, m).Rod(0.5f, 0.8f, 0.11f, m).Rod(0.8f, 1.05f, 0.06f, m).Ball(0.05f, 0, 0, 1.06f, GoopDark));
        W("Pitchfork", St, None, Wood, Metal, "Farm tool turned jungle weapon.",
            (m, a) =>
            {
                var r = new Recipe().Rod(-0.45f, 0.9f, 0.04f, m).Box(0.3f, 0.03f, 0.03f, 0, 0, 0.92f, a);
                foreach (float x in new[] { -0.135f, -0.045f, 0.045f, 0.135f }) r.Rod(0.92f, 1.15f, 0.02f, a, x);
                return r;
            });
        W("Broom Staff", St, WeaponEffect.Smoke, Wood, Straw, "Sweeps up a dust cloud when it hits.",
            (m, a) => new Recipe().Rod(-0.45f, 0.85f, 0.04f, m).DiscZ(0.08f, 0.04f, 0.86f, Red).Egg(0.26f, 0.08f, 0.32f, 0, 0, 1.03f, a));
        W("Totem Staff", St, WeaponEffect.Shock, Wood, Teal, "Carved faces that zap whoever they poke.",
            (m, a) => new Recipe().Rod(-0.45f, 0.8f, 0.05f, m).Box(0.14f, 0.14f, 0.14f, 0, 0, 0.88f, a)
                .Box(0.12f, 0.12f, 0.12f, 0, 0, 1.0f, Gold, 0, 0, 45).Box(0.15f, 0.1f, 0.1f, 0, 0, 1.11f, Red)
                .Ball(0.03f, -0.03f, 0.075f, 0.88f, Black).Ball(0.03f, 0.03f, 0.075f, 0.88f, Black));
        W("Fishing Harpoon", St, None, H("4F7CAC"), Metal, "For very big fish. Or gorillas.",
            (m, a) => new Recipe().Rod(-0.4f, 0.95f, 0.035f, m).Box(0.06f, 0.06f, 0.14f, 0, 0, 1.02f, a, 0, 0, 45)
                .Box(0.015f, 0.015f, 0.08f, 0.04f, 0, 0.95f, a, 0, -30).Box(0.015f, 0.015f, 0.08f, -0.04f, 0, 0.95f, a, 0, 30)
                .Add(PrimitiveType.Cylinder, new Vector3(0.1f, 0.02f, 0.1f), new Vector3(0, 0, -0.3f), Cream, new Vector3(90, 0, 0)));
        W("Sugar Cane Pike", St, None, Red, White, "Sweet and pointy.",
            (m, a) =>
            {
                var r = new Recipe();
                for (int i = 0; i < 10; i++) r.Rod(-0.45f + i * 0.15f, -0.3f + i * 0.15f, 0.045f, i % 2 == 0 ? m : a);
                return r.Box(0.06f, 0.06f, 0.12f, 0, 0, 1.1f, a, 0, 0, 45);
            });
        W("Thorn Javelin", St, None, Leaf, H("A1887F"), "Covered in thorns. Don't hold the pointy end.",
            (m, a) =>
            {
                var r = new Recipe().Rod(-0.45f, 1.0f, 0.035f, m).Box(0.05f, 0.05f, 0.14f, 0, 0, 1.06f, a, 0, 0, 45);
                for (int i = 0; i < 5; i++) r.Box(0.02f, 0.02f, 0.05f, (i % 2 == 0 ? 0.03f : -0.03f), 0, 0.3f + i * 0.14f, a, 0, i % 2 == 0 ? -40 : 40);
                return r;
            });
        W("Lightning Rod", St, WeaponEffect.Shock, Metal, Yellow, "Crackles with storm power.",
            (m, a) => new Recipe().Rod(-0.45f, 0.9f, 0.035f, m).DiscZ(0.07f, 0.03f, 0.4f, H("C87533")).DiscZ(0.07f, 0.03f, 0.5f, H("C87533"))
                .Ball(0.08f, 0, 0, 0.95f, a).Box(0.02f, 0.02f, 0.1f, 0.03f, 0, 1.03f, a, 0, 30).Box(0.02f, 0.02f, 0.1f, -0.03f, 0, 1.1f, a, 0, -30),
            d => { d.glows = true; });

        // ================= 4. THROWABLES =================
        Cat("Throwables");
        W("Goop Grenade", T, WeaponEffect.Sticky, Goop, Metal, "Throw it. Everyone nearby gets gooped.",
            (m, a) => new Recipe().Ball(0.13f, 0, 0, 0.06f, m).Box(0.05f, 0.04f, 0.04f, 0, 0.07f, 0.06f, a).DiscZ(0.04f, 0.008f, 0.06f, a, 0.04f, 0.09f),
            d => { d.areaRadius = 2.5f; d.singleUse = true; });
        W("Bounce Blob", T, WeaponEffect.Bounce, Purple, Black, "A happy blob that launches whatever it lands on.",
            (m, a) => new Recipe().Egg(0.16f, 0.13f, 0.16f, 0, 0, 0.06f, m).Ball(0.03f, -0.03f, 0.04f, 0.13f, a).Ball(0.03f, 0.03f, 0.04f, 0.13f, a),
            d => { d.damage = 6f; });
        W("Coconut Bomb", T, None, Coconut, Yellow, "Kaboom, coconut style.",
            (m, a) => new Recipe().Ball(0.16f, 0, 0, 0.07f, m).Post(0.08f, 0.13f, 0.015f, Cream, 0, 0.07f).Ball(0.035f, 0, 0.14f, 0.07f, a),
            d => { d.areaRadius = 2.5f; d.singleUse = true; d.damage = 28f; });
        W("Banana Peel Trap", T, WeaponEffect.Shock, Banana, Cream, "Throw it at their feet. They slip and get dizzy.",
            (m, a) => new Recipe().Ball(0.06f, 0, 0, 0.05f, a)
                .Add(PrimitiveType.Capsule, new Vector3(0.05f, 0.08f, 0.02f), new Vector3(0.05f, -0.01f, 0.05f), m, new Vector3(0, 0, 70))
                .Add(PrimitiveType.Capsule, new Vector3(0.05f, 0.08f, 0.02f), new Vector3(-0.05f, -0.01f, 0.05f), m, new Vector3(0, 0, -70))
                .Add(PrimitiveType.Capsule, new Vector3(0.05f, 0.08f, 0.02f), new Vector3(0, -0.01f, 0.1f), m, new Vector3(70, 0, 0)),
            d => { d.singleUse = true; d.damage = 5f; });
        W("Sticky Splat Ball", T, WeaponEffect.Sticky, H("7CFC00"), GoopDark, "Splats into a puddle of sticky goop.",
            (m, a) => new Recipe().Ball(0.12f, 0, 0, 0.06f, m).Ball(0.05f, 0.06f, 0.02f, 0.06f, a).Ball(0.04f, -0.05f, -0.03f, 0.08f, a).Ball(0.045f, 0, 0.06f, 0.03f, a),
            d => { d.areaRadius = 1.5f; d.singleUse = true; d.damage = 8f; });
        W("Smoke Puffball", T, WeaponEffect.Smoke, H("A0A0A0"), White, "Pops into a big smoke cloud. Great for sneaking.",
            (m, a) => new Recipe().Ball(0.14f, 0, 0, 0.07f, m).Ball(0.07f, 0.06f, 0.05f, 0.07f, a).Ball(0.06f, -0.06f, 0.04f, 0.09f, a),
            d => { d.singleUse = true; d.damage = 0f; d.knockback = 0f; });
        W("Boomerang", T, None, Orange, Blue, "Throw it and it comes back to you!",
            (m, a) => new Recipe().Box(0.05f, 0.02f, 0.3f, -0.09f, 0, 0.1f, m, 0, -40).Box(0.05f, 0.02f, 0.3f, 0.09f, 0, 0.1f, m, 0, 40)
                .Box(0.052f, 0.022f, 0.05f, -0.15f, 0, 0.03f, a, 0, -40).Box(0.052f, 0.022f, 0.05f, 0.15f, 0, 0.03f, a, 0, 40),
            d => { d.returnsAfterThrow = true; d.damage = 15f; });
        W("Leaf Throwing Stars", T, None, Leaf, Gold, "Spinning leaves that sting.",
            (m, a) => new Recipe().Box(0.22f, 0.01f, 0.05f, 0, 0, 0.06f, m).Box(0.05f, 0.01f, 0.22f, 0, 0, 0.06f, m)
                .Box(0.16f, 0.012f, 0.04f, 0, 0, 0.06f, a, 0, 45).Box(0.04f, 0.012f, 0.16f, 0, 0, 0.06f, a, 0, 45),
            d => { d.damage = 12f; d.mass = 0.2f; });
        W("Firecracker Fruit", T, WeaponEffect.Fire, Red, Yellow, "A spicy fruit that pops with fire.",
            (m, a) => new Recipe().Egg(0.12f, 0.14f, 0.12f, 0, 0, 0.06f, m).Post(0.07f, 0.11f, 0.015f, Leaf, 0, 0.06f)
                .Box(0.125f, 0.02f, 0.125f, 0, 0.02f, 0.06f, a, 0, 45).Box(0.125f, 0.02f, 0.125f, 0, -0.03f, 0.06f, a),
            d => { d.areaRadius = 2f; d.singleUse = true; });
        W("Snowball", T, WeaponEffect.Freeze, White, Ice, "Brrr! Freezes whoever it hits.",
            (m, a) => new Recipe().Ball(0.12f, 0, 0, 0.06f, m).Ball(0.05f, 0.04f, 0.04f, 0.06f, a),
            d => { d.singleUse = true; d.damage = 8f; });

        // ================= 5. BLASTERS =================
        Cat("Blasters");
        W("Goop Gun", Sh, WeaponEffect.Sticky, Goop, Iron, "Shoots blobs of goop that slow targets.",
            (m, a) => new Recipe().Grip(a).Egg(0.1f, 0.11f, 0.28f, 0, 0.06f, 0.1f, m).Rod(0.2f, 0.36f, 0.04f, a, 0, 0.06f).Ball(0.09f, 0, 0.14f, 0.02f, GoopDark));
        W("Bubble Blaster", Sh, WeaponEffect.Bounce, H("7EC8FF"), White, "Big slow bubbles that pop and bounce people.",
            (m, a) => new Recipe().Grip(Blue).Egg(0.1f, 0.1f, 0.24f, 0, 0.06f, 0.1f, m).DiscZ(0.13f, 0.02f, 0.3f, a, 0, 0.06f).Rod(0.2f, 0.29f, 0.03f, a, 0, 0.06f),
            d => { d.projectileSpeed = 8f; d.projectileSize = 0.22f; d.cooldown = 0.5f; d.damage = 3f; });
        W("Seed Spitter", Sh, None, Wood, Bamboo, "Rapid-fire seeds. Pew pew pew.",
            (m, a) => new Recipe().Grip(DarkWood).Rod(-0.05f, 0.35f, 0.06f, m, 0, 0.05f).Box(0.06f, 0.08f, 0.08f, 0, 0.11f, 0.05f, a),
            d => { d.projectileSpeed = 30f; d.projectileSize = 0.04f; d.projectileGravity = true; d.cooldown = 0.15f; d.damage = 4f; });
        W("Slingshot", Sh, None, Wood, Red, "Launches pebbles in a nice arc.",
            (m, a) => new Recipe().Rod(-0.1f, 0.12f, 0.04f, m)
                .Add(PrimitiveType.Cylinder, new Vector3(0.03f, 0.07f, 0.03f), new Vector3(0.045f, 0, 0.18f), m, new Vector3(90, 0, -25))
                .Add(PrimitiveType.Cylinder, new Vector3(0.03f, 0.07f, 0.03f), new Vector3(-0.045f, 0, 0.18f), m, new Vector3(90, 0, 25))
                .Box(0.13f, 0.012f, 0.012f, 0, 0, 0.245f, a),
            d => { d.projectileSpeed = 20f; d.projectileGravity = true; d.damage = 12f; d.cooldown = 0.7f; d.projectileSize = 0.07f; });
        W("Water Squirter", Sh, None, Cyan, Yellow, "Soak 'em! Pushes more than it hurts.",
            (m, a) => new Recipe().Grip(a).Egg(0.1f, 0.12f, 0.22f, 0, 0.06f, 0.08f, m).Ball(0.1f, 0, 0.14f, 0.04f, a).Rod(0.18f, 0.3f, 0.02f, a, 0, 0.06f),
            d => { d.knockback = 4f; d.projectileSize = 0.06f; d.cooldown = 0.12f; d.damage = 2f; d.projectileSpeed = 18f; });
        W("Dart Blowpipe", Sh, WeaponEffect.Sticky, Bamboo, Red, "Sleepy darts make targets slow and wobbly.",
            (m, a) => new Recipe().Rod(-0.2f, 0.6f, 0.03f, m).DiscZ(0.04f, 0.03f, -0.19f, a).DiscZ(0.038f, 0.02f, 0.2f, BambooRing).DiscZ(0.038f, 0.02f, 0.45f, BambooRing),
            d => { d.projectileShape = PrimitiveType.Capsule; d.projectileSpeed = 35f; d.damage = 10f; d.cooldown = 0.8f; d.projectileSize = 0.08f; });
        W("Confetti Cannon", Sh, WeaponEffect.Silly, Purple, Gold, "It's a party! A very loud party.",
            (m, a) => new Recipe().Grip(a).Rod(-0.02f, 0.3f, 0.12f, m, 0, 0.05f).DiscZ(0.125f, 0.03f, 0.1f, a, 0, 0.05f).DiscZ(0.125f, 0.03f, 0.22f, a, 0, 0.05f),
            d => { d.damage = 2f; d.projectileSize = 0.15f; d.areaRadius = 1.2f; d.cooldown = 0.6f; });
        W("Pea Shooter", Sh, None, Leaf, H("7CB342"), "Fast little peas. Lots of them.",
            (m, a) => new Recipe().Grip(DarkWood).Rod(-0.02f, 0.38f, 0.05f, m, 0, 0.05f).Ball(0.09f, 0, 0.05f, 0.4f, a)
                .Box(0.08f, 0.01f, 0.04f, 0.04f, 0.08f, 0.15f, a, 0, 0, 30),
            d => { d.cooldown = 0.18f; d.damage = 4f; d.projectileSize = 0.06f; });
        W("Foam Launcher", Sh, WeaponEffect.Bounce, Orange, Blue, "Soft foam darts that knock people back.",
            (m, a) => new Recipe().Grip(a).Box(0.1f, 0.12f, 0.3f, 0, 0.06f, 0.12f, m).Rod(0.27f, 0.4f, 0.07f, a, 0, 0.06f),
            d => { d.projectileShape = PrimitiveType.Capsule; d.projectileSize = 0.12f; d.damage = 3f; });
        W("Glitter Ray", Sh, WeaponEffect.Silly, Pink, Gold, "Zaps out sparkles and squeaks.",
            (m, a) => new Recipe().Grip(Purple).Ball(0.12f, 0, 0.06f, 0.05f, m).Rod(0.05f, 0.3f, 0.03f, a, 0, 0.06f)
                .DiscZ(0.07f, 0.015f, 0.14f, m, 0, 0.06f).DiscZ(0.06f, 0.015f, 0.2f, m, 0, 0.06f).DiscZ(0.05f, 0.015f, 0.26f, m, 0, 0.06f).Ball(0.04f, 0, 0.06f, 0.31f, White),
            d => { d.glows = true; d.projectileSpeed = 40f; d.damage = 5f; d.cooldown = 0.2f; d.projectileSize = 0.05f; });

        // ================= 6. HEAVY HITTERS =================
        Cat("Heavy Hitters");
        W("Boulder Launcher", Sh, None, Stone, Iron, "Lobs huge boulders. Slow, but wow.",
            (m, a) => new Recipe().Grip(a).Rod(-0.05f, 0.55f, 0.18f, m, 0, 0.1f).DiscZ(0.23f, 0.05f, 0.55f, a, 0, 0.1f)
                .Add(PrimitiveType.Cylinder, new Vector3(0.16f, 0.02f, 0.16f), new Vector3(0.11f, 0.06f, 0.05f), Wood, new Vector3(0, 0, 90))
                .Add(PrimitiveType.Cylinder, new Vector3(0.16f, 0.02f, 0.16f), new Vector3(-0.11f, 0.06f, 0.05f), Wood, new Vector3(0, 0, 90)),
            d => { d.projectileGravity = true; d.areaRadius = 2f; d.projectileSize = 0.35f; d.projectileSpeed = 14f; });
        W("Melon Mortar", Sh, None, H("2E8B57"), H("90EE90"), "Fires whole watermelons. Splat!",
            (m, a) => new Recipe().Grip(Iron).Rod(-0.05f, 0.4f, 0.2f, m, 0, 0.1f).Box(0.205f, 0.04f, 0.4f, 0, 0.1f, 0.18f, a).DiscZ(0.22f, 0.03f, 0.4f, Iron, 0, 0.1f),
            d => { d.projectileGravity = true; d.areaRadius = 2.5f; d.projectileSize = 0.3f; d.projectileSpeed = 12f; });
        W("Gorilla Fist Gauntlets", S, None, DarkWood, Tan, "Giant fists. Punch for big knockback.",
            (m, a) => new Recipe().Rod(-0.12f, 0.03f, 0.15f, a).Box(0.2f, 0.16f, 0.18f, 0, 0, 0.12f, m)
                .Ball(0.06f, -0.06f, 0.04f, 0.21f, a).Ball(0.06f, 0, 0.04f, 0.215f, a).Ball(0.06f, 0.06f, 0.04f, 0.21f, a),
            d => { d.knockback = 12f; d.range = 0.25f; });
        W("Chain Wrecking Ball", S, None, Black, Metal, "A wrecking ball on a chain. Swing big!",
            (m, a) =>
            {
                var r = new Recipe().Handle(Iron);
                for (int i = 0; i < 5; i++) r.Ball(0.05f, 0, 0, 0.18f + i * 0.08f, a);
                return r.Ball(0.3f, 0, 0, 0.72f, m).Box(0.31f, 0.04f, 0.04f, 0, 0, 0.72f, Gold);
            },
            d => { d.range = 0.3f; d.damage *= 1.15f; });
        W("Anvil Dropper", Sh, WeaponEffect.Shock, Iron, Gold, "Drops anvils. Targets see stars.",
            (m, a) => new Recipe().Grip(a).Box(0.16f, 0.16f, 0.3f, 0, 0.08f, 0.1f, m).Box(0.2f, 0.05f, 0.12f, 0, 0.18f, 0.1f, a),
            d => { d.projectileGravity = true; d.projectileShape = PrimitiveType.Cube; d.projectileSize = 0.3f; d.projectileSpeed = 10f; d.cooldown = 2f; });
        W("Tree Trunk Ram", S, None, Wood, Leaf, "Basically a whole tree. Two hands recommended.",
            (m, a) => new Recipe().Rod(-0.3f, 1.1f, 0.24f, m).DiscZ(0.25f, 0.03f, 0.2f, DarkWood).DiscZ(0.25f, 0.03f, 0.7f, DarkWood)
                .Add(PrimitiveType.Cylinder, new Vector3(0.05f, 0.12f, 0.05f), new Vector3(0.15f, 0.08f, 0.5f), m, new Vector3(0, 0, -50))
                .Ball(0.12f, 0.24f, 0.17f, 0.5f, a),
            d => { d.range = 0.3f; d.knockback = 12f; d.mass = 6f; });
        W("Goop Cannon", Sh, WeaponEffect.Sticky, Goop, Iron, "Big goop balls that splash everywhere.",
            (m, a) => new Recipe().Grip(a).Rod(-0.05f, 0.5f, 0.2f, m, 0, 0.1f).DiscZ(0.24f, 0.05f, 0.5f, a, 0, 0.1f).Ball(0.08f, 0, 0.0f, 0.52f, GoopDark),
            d => { d.projectileGravity = true; d.areaRadius = 2f; d.projectileSize = 0.3f; });
        W("Stomp Boots", S, WeaponEffect.Bounce, Red, Black, "Kick-boots that launch targets sky high.",
            (m, a) => new Recipe().Box(0.14f, 0.12f, 0.26f, 0, 0, 0.08f, m).Box(0.16f, 0.04f, 0.3f, 0, -0.07f, 0.08f, a).Post(0.04f, 0.2f, 0.12f, m, 0, -0.02f),
            d => { d.range = 0.22f; });
        W("Thunder Drum", S, WeaponEffect.Shock, Red, White, "BOOM goes the drum, ZAP goes the target.",
            (m, a) => new Recipe().Handle(Wood).Rod(0.14f, 0.42f, 0.3f, m).DiscZ(0.31f, 0.02f, 0.14f, a).DiscZ(0.31f, 0.02f, 0.42f, a)
                .Box(0.02f, 0.1f, 0.02f, 0.155f, 0, 0.28f, Yellow, 0, 0, 30).Box(0.02f, 0.1f, 0.02f, -0.155f, 0, 0.28f, Yellow, 0, 0, -30));
        W("Big Bonk Hammer", S, None, Red, Yellow, "The biggest bonk on the island.",
            (m, a) => new Recipe().Rod(-0.2f, 0.8f, 0.06f, Wood).Post(-0.25f, 0.25f, 0.32f, m, 0, 0.92f)
                .DiscY(0.33f, 0.03f, 0, 0.25f, 0.92f, a).DiscY(0.33f, 0.03f, 0, -0.25f, 0.92f, a),
            d => { d.range = 0.3f; d.damage *= 1.2f; d.knockback = 14f; });

        // ================= 7. ELEMENTAL =================
        Cat("Elemental");
        W("Fire Torch", Sh, WeaponEffect.Fire, Orange, Yellow, "Throws little fireballs.",
            (m, a) => new Recipe().Rod(-0.15f, 0.35f, 0.05f, Wood).Rod(0.35f, 0.45f, 0.1f, Iron)
                .Egg(0.12f, 0.12f, 0.2f, 0, 0, 0.52f, m).Ball(0.07f, 0, 0, 0.5f, a),
            d => { d.glows = true; d.projectileSpeed = 14f; d.projectileSize = 0.12f; });
        W("Ice Wand", Sh, WeaponEffect.Freeze, Ice, White, "Shoots frosty sparkles that freeze.",
            (m, a) => new Recipe().Rod(-0.12f, 0.35f, 0.03f, a).Box(0.06f, 0.06f, 0.12f, 0, 0, 0.42f, m, 45, 45),
            d => { d.projectileSize = 0.07f; });
        W("Lightning Staff", Sh, WeaponEffect.Shock, Purple, Yellow, "Throws bolts of lightning.",
            (m, a) => new Recipe().Rod(-0.4f, 0.8f, 0.04f, m).Ball(0.12f, 0, 0, 0.88f, a)
                .Box(0.02f, 0.02f, 0.1f, 0.05f, 0, 0.78f, a, 0, 30).Box(0.02f, 0.02f, 0.1f, -0.05f, 0, 0.78f, a, 0, -30),
            d => { d.glows = true; d.projectileSpeed = 35f; d.projectileShape = PrimitiveType.Capsule; });
        W("Wind Fan", Sh, WeaponEffect.Bounce, Teal, White, "Blows a gust that knocks people back.",
            (m, a) =>
            {
                var r = new Recipe().Handle(Wood, 0.24f);
                for (int i = 0; i < 5; i++)
                {
                    float ang = -50f + i * 25f;
                    Vector3 dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                    r.Box(0.04f, 0.01f, 0.28f, dir.x * 0.14f, 0, 0.12f + dir.z * 0.14f, i % 2 == 0 ? m : a, 0, ang);
                }
                return r;
            },
            d => { d.damage = 1f; d.projectileSpeed = 12f; d.projectileSize = 0.3f; d.knockback = 6f; d.range = 12f; });
        W("Lava Ladle", Sh, WeaponEffect.Fire, Orange, Black, "Flings scoops of hot lava.",
            (m, a) => new Recipe().Rod(-0.15f, 0.45f, 0.035f, a).Egg(0.17f, 0.08f, 0.17f, 0, 0.03f, 0.52f, Iron).DiscY(0.14f, 0.02f, 0, 0.07f, 0.52f, m),
            d => { d.glows = true; d.projectileGravity = true; d.projectileSize = 0.12f; d.projectileSpeed = 12f; });
        W("Frost Mitts", U, WeaponEffect.Freeze, Ice, White, "Clap them to freeze everyone close to you.",
            (m, a) => new Recipe().Egg(0.14f, 0.1f, 0.2f, 0, 0, 0.06f, m).Egg(0.05f, 0.05f, 0.1f, 0.07f, 0.02f, 0.02f, m, 0, 30).Rod(-0.1f, -0.02f, 0.13f, a),
            d => { d.affectsArea = true; d.range = 3f; d.damage = 8f; d.cooldown = 6f; });
        W("Storm Cloud Jar", U, WeaponEffect.Shock, H("BFE6FF"), Wood, "Open the lid to zap everyone around you.",
            (m, a) => new Recipe().Rod(-0.02f, 0.2f, 0.14f, m).DiscZ(0.15f, 0.03f, 0.21f, a)
                .Ball(0.07f, 0.02f, 0.02f, 0.08f, H("707A85")).Ball(0.06f, -0.02f, 0, 0.12f, H("707A85")),
            d => { d.affectsArea = true; d.range = 4f; d.damage = 10f; d.cooldown = 8f; });
        W("Sunbeam Mirror", Sh, WeaponEffect.Fire, H("FFF59D"), Gold, "Bounces a hot sunbeam at targets.",
            (m, a) => new Recipe().Handle(a, 0.22f).DiscZ(0.28f, 0.02f, 0.25f, a).DiscZ(0.25f, 0.025f, 0.255f, m),
            d => { d.glows = true; d.projectileSpeed = 45f; d.projectileSize = 0.06f; d.projectileShape = PrimitiveType.Capsule; });
        W("Moon Rock Orb", U, WeaponEffect.Bounce, H("D6CCF2"), Stone, "Squeeze it for a floaty moon jump.",
            (m, a) => new Recipe().Ball(0.16f, 0, 0, 0.06f, m).Ball(0.04f, 0.05f, 0.04f, 0.1f, a).Ball(0.03f, -0.05f, 0.02f, 0.11f, a).Ball(0.035f, 0, -0.05f, 0.1f, a),
            d => { d.knockback = 12f; d.range = 0f; d.cooldown = 4f; d.glows = true; });
        W("Earthquake Totem", U, WeaponEffect.Bounce, Wood, Orange, "Shakes the ground and bounces everyone nearby.",
            (m, a) => new Recipe().Box(0.12f, 0.12f, 0.12f, 0, 0, 0.0f, m).Box(0.12f, 0.12f, 0.12f, 0, 0, 0.12f, a).Box(0.14f, 0.1f, 0.12f, 0, 0, 0.24f, m)
                .Box(0.04f, 0.02f, 0.01f, 0, 0.02f, 0.30f, Black),
            d => { d.affectsArea = true; d.range = 5f; d.knockback = 8f; d.damage = 10f; d.cooldown = 10f; });

        // ================= 8. TRAPS & GADGETS =================
        Cat("Traps & Gadgets");
        W("Sticky Goop Mine", Tr, WeaponEffect.Sticky, Goop, Red, "Press trigger to drop it. Whoever steps near gets gooped.",
            (m, a) => new Recipe().DiscY(0.22f, 0.05f, 0, 0, 0.06f, m).Egg(0.12f, 0.06f, 0.12f, 0, 0.03f, 0.06f, GoopDark).Ball(0.03f, 0, 0.06f, 0.06f, a));
        W("Net Launcher", U, WeaponEffect.Sticky, H("8D6E63"), Straw, "Tosses a net over everyone near you.",
            (m, a) => new Recipe().Grip(Iron).Box(0.1f, 0.1f, 0.24f, 0, 0.06f, 0.1f, m).Box(0.12f, 0.03f, 0.12f, 0, 0.12f, 0.12f, a, 0, 45),
            d => { d.affectsArea = true; d.range = 4f; d.cooldown = 6f; d.damage = 3f; });
        W("Spring Trap", Tr, WeaponEffect.Bounce, Gold, Metal, "A launch pad! Works on enemies... and you.",
            (m, a) =>
            {
                var r = new Recipe().Box(0.22f, 0.03f, 0.22f, 0, -0.06f, 0.08f, Iron);
                for (int i = 0; i < 4; i++) r.DiscY(0.12f, 0.015f, 0, -0.04f + i * 0.025f, 0.08f, a);
                return r.Box(0.2f, 0.03f, 0.2f, 0, 0.06f, 0.08f, m);
            },
            d => { d.knockback = 8f; d.damage = 2f; d.singleUse = false; d.range = 1.0f; });
        W("Bear Hug Trap", Tr, WeaponEffect.Freeze, Pink, White, "Fluffy arms that hug you and won't let go (for a bit).",
            (m, a) => new Recipe().DiscY(0.24f, 0.03f, 0, -0.05f, 0.08f, a)
                .Box(0.22f, 0.12f, 0.03f, 0, 0.02f, 0.0f, m, -30).Box(0.22f, 0.12f, 0.03f, 0, 0.02f, 0.16f, m, 30)
                .Ball(0.05f, 0.08f, 0.07f, 0.03f, a).Ball(0.05f, -0.08f, 0.07f, 0.13f, a));
        W("Tripwire Vine", Tr, WeaponEffect.Shock, Leaf, Wood, "Trip! Leaves them dizzy.",
            (m, a) => new Recipe().Post(-0.08f, 0.1f, 0.025f, a, -0.15f, 0.08f).Post(-0.08f, 0.1f, 0.025f, a, 0.15f, 0.08f)
                .Bar(-0.15f, 0.15f, 0.015f, m, 0.05f, 0.08f).Box(0.04f, 0.01f, 0.03f, 0.05f, 0.06f, 0.08f, m, 0, 30));
        W("Decoy Gorilla", Tr, WeaponEffect.Silly, DarkWood, Tan, "A tiny fake gorilla that honks when someone gets close.",
            (m, a) => new Recipe().Egg(0.16f, 0.2f, 0.13f, 0, 0, 0.06f, m).Ball(0.12f, 0, 0.15f, 0.06f, m).Egg(0.08f, 0.06f, 0.03f, 0, 0.14f, 0.12f, a)
                .Add(PrimitiveType.Capsule, new Vector3(0.05f, 0.09f, 0.05f), new Vector3(0.1f, -0.02f, 0.06f), m, new Vector3(0, 0, 20))
                .Add(PrimitiveType.Capsule, new Vector3(0.05f, 0.09f, 0.05f), new Vector3(-0.1f, -0.02f, 0.06f), m, new Vector3(0, 0, -20)),
            d => { d.damage = 0f; d.knockback = 0f; d.range = 1.6f; });
        W("Beehive Bomb", Tr, WeaponEffect.Fire, Yellow, Coconut, "Angry bees sting everyone nearby.",
            (m, a) => new Recipe().Egg(0.16f, 0.2f, 0.16f, 0, 0, 0.07f, m).DiscY(0.165f, 0.02f, 0, 0.04f, 0.07f, a)
                .DiscY(0.165f, 0.02f, 0, -0.03f, 0.07f, a).Ball(0.04f, 0, -0.01f, 0.15f, Black),
            d => { d.areaRadius = 2f; });
        W("Magnet Glove", U, WeaponEffect.None, Red, Metal, "Pulls loose stuff and players toward you.",
            (m, a) => new Recipe().Box(0.1f, 0.06f, 0.14f, 0, 0, 0.02f, Blue)
                .Box(0.04f, 0.04f, 0.14f, 0.05f, 0, 0.15f, m).Box(0.04f, 0.04f, 0.14f, -0.05f, 0, 0.15f, m)
                .Box(0.042f, 0.042f, 0.04f, 0.05f, 0, 0.23f, a).Box(0.042f, 0.042f, 0.04f, -0.05f, 0, 0.23f, a),
            d => { d.affectsArea = true; d.range = 5f; d.knockback = -6f; d.damage = 0f; d.cooldown = 3f; });
        W("Freeze Trap", Tr, WeaponEffect.Freeze, Ice, Blue, "Ice crystals that freeze anyone who walks by.",
            (m, a) => new Recipe().DiscY(0.22f, 0.03f, 0, -0.04f, 0.08f, a).Box(0.05f, 0.14f, 0.05f, 0, 0.03f, 0.08f, m, 0, 45)
                .Box(0.04f, 0.1f, 0.04f, 0.06f, 0.01f, 0.06f, m, 0, 0, -25).Box(0.04f, 0.1f, 0.04f, -0.05f, 0.01f, 0.11f, m, 0, 0, 25),
            d => { d.range = 1.6f; d.damage = 5f; });
        W("Teleport Banana", U, WeaponEffect.Teleport, Purple, Yellow, "Eat it and blink forward a few meters!",
            (m, a) => new Recipe()
                .Add(PrimitiveType.Capsule, new Vector3(0.07f, 0.1f, 0.07f), new Vector3(0, 0.0f, 0.08f), m, new Vector3(80, 0, 0))
                .Add(PrimitiveType.Capsule, new Vector3(0.065f, 0.09f, 0.065f), new Vector3(0, 0.05f, 0.22f), m, new Vector3(60, 0, 0))
                .Ball(0.03f, 0, 0.1f, 0.31f, a),
            d => { d.range = 5f; d.singleUse = true; d.cooldown = 1f; });

        // ================= 9. SUPPORT =================
        Cat("Support");
        W("Healing Mango", U, WeaponEffect.Heal, Orange, Leaf, "Eat it to get 40 HP back.",
            (m, a) => new Recipe().Egg(0.12f, 0.15f, 0.1f, 0, 0, 0.06f, m).Box(0.06f, 0.01f, 0.03f, 0.02f, 0.08f, 0.06f, a, 0, 0, -20).Post(0.07f, 0.1f, 0.012f, Wood, 0, 0.06f),
            d => { d.damage = 40f; d.singleUse = true; d.cooldown = 1f; });
        W("Shield Leaf", U, WeaponEffect.Shield, Leaf, H("2E7D32"), "Hold it up to block all damage for 5 seconds.",
            (m, a) => new Recipe().Handle(Wood, 0.2f).Egg(0.35f, 0.45f, 0.02f, 0, 0.1f, 0.12f, m).Box(0.01f, 0.4f, 0.025f, 0, 0.1f, 0.12f, a),
            d => { d.cooldown = 10f; });
        W("Grapple Vine", U, WeaponEffect.Bounce, Leaf, Metal, "Point and squeeze to zip where you're aiming.",
            (m, a) =>
            {
                var r = new Recipe();
                for (int i = 0; i < 4; i++) r.DiscZ(0.1f, 0.025f, i * 0.03f, m);
                return r.Rod(0.1f, 0.25f, 0.02f, m).Box(0.02f, 0.02f, 0.08f, 0.03f, 0, 0.27f, a, 0, -40).Box(0.02f, 0.02f, 0.08f, -0.03f, 0, 0.27f, a, 0, 40);
            },
            d => { d.knockback = 4f; d.range = 12f; d.cooldown = 3f; });
        W("Speed Juice", U, WeaponEffect.Speed, Cyan, Red, "Drink it to run super fast for 6 seconds.",
            (m, a) => new Recipe().Rod(-0.08f, 0.12f, 0.08f, m).Rod(0.12f, 0.18f, 0.035f, m).DiscZ(0.045f, 0.03f, 0.19f, a).Box(0.081f, 0.06f, 0.08f, 0, 0, 0.02f, White),
            d => { d.singleUse = true; d.cooldown = 1f; });
        W("Goop Wall Spray", U, WeaponEffect.None, Goop, Iron, "Sprays a climbable goop wall in front of you. Lasts 20 seconds.",
            (m, a) => new Recipe().Rod(-0.1f, 0.15f, 0.08f, m).DiscZ(0.082f, 0.03f, 0.16f, a).Box(0.03f, 0.03f, 0.04f, 0, 0.02f, 0.19f, Black),
            d => { d.spawnsBlock = true; d.cooldown = 4f; });
        W("Jump Mushroom", U, WeaponEffect.Bounce, Red, Cream, "Squeeze for a super jump!",
            (m, a) => new Recipe().Rod(-0.08f, 0.1f, 0.06f, a).Egg(0.2f, 0.2f, 0.1f, 0, 0, 0.12f, m)
                .Ball(0.04f, 0.05f, 0.04f, 0.16f, White).Ball(0.035f, -0.05f, 0.02f, 0.16f, White).Ball(0.03f, 0, -0.06f, 0.16f, White),
            d => { d.knockback = 9f; d.range = 0f; d.cooldown = 3f; });
        W("Glow Stick", U, WeaponEffect.None, H("B2FF59"), White, "Squeeze to turn the light on and off.",
            (m, a) => new Recipe().Rod(-0.12f, 0.18f, 0.03f, m).DiscZ(0.032f, 0.02f, -0.12f, a).DiscZ(0.032f, 0.02f, 0.18f, a),
            d => { d.glows = true; d.cooldown = 0.3f; });
        W("Camo Leaf Cloak", U, WeaponEffect.Smoke, Leaf, H("33691E"), "Puff! Hide in a cloud of leaves and smoke.",
            (m, a) => new Recipe().Egg(0.16f, 0.02f, 0.22f, 0, 0, 0.06f, m, 0, 20).Egg(0.15f, 0.02f, 0.2f, 0.02f, 0.02f, 0.08f, a, 0, -30, 10)
                .Egg(0.14f, 0.02f, 0.2f, -0.02f, 0.04f, 0.04f, m, 0, 70, -10),
            d => { d.cooldown = 10f; });
        W("Radar Coconut", U, WeaponEffect.Silly, Coconut, Red, "Pings! Everyone within 30 m gets a confetti marker.",
            (m, a) => new Recipe().Ball(0.15f, 0, 0, 0.06f, m).Post(0.07f, 0.2f, 0.01f, Metal, 0, 0.06f).Ball(0.03f, 0, 0.2f, 0.06f, a)
                .Add(PrimitiveType.Cylinder, new Vector3(0.08f, 0.005f, 0.08f), new Vector3(0.06f, 0.06f, 0.06f), Metal, new Vector3(0, 0, 60)),
            d => { d.affectsArea = true; d.range = 30f; d.damage = 0f; d.knockback = 0f; d.cooldown = 10f; });
        W("Revive Totem", U, WeaponEffect.Heal, Gold, Teal, "Restores ALL your HP. Long recharge.",
            (m, a) => new Recipe().Box(0.1f, 0.1f, 0.1f, 0, 0, 0, a).Box(0.09f, 0.09f, 0.09f, 0, 0, 0.1f, m).Box(0.08f, 0.08f, 0.08f, 0, 0, 0.19f, a)
                .Box(0.12f, 0.02f, 0.05f, 0.09f, 0, 0.1f, m, 0, 0, 20).Box(0.12f, 0.02f, 0.05f, -0.09f, 0, 0.1f, m, 0, 0, -20),
            d => { d.damage = 100f; d.cooldown = 30f; });

        // ================= 10. SILLY =================
        Cat("Silly");
        W("Rubber Chicken", S, WeaponEffect.Silly, Yellow, Orange, "SQUAWK! Every hit is a party.",
            (m, a) => new Recipe().Rod(-0.12f, 0.08f, 0.02f, a).Egg(0.09f, 0.09f, 0.32f, 0, 0, 0.25f, m).Pill(0.38f, 0.48f, 0.05f, m)
                .Ball(0.09f, 0, 0.02f, 0.52f, m).Box(0.03f, 0.02f, 0.05f, 0, 0.01f, 0.58f, a).Box(0.015f, 0.05f, 0.05f, 0, 0.07f, 0.52f, Red),
            d => { d.mass = 0.5f; });
        W("Squeaky Hammer", S, WeaponEffect.Silly, Red, Yellow, "Squeak squeak. Doesn't hurt much. Very funny.",
            (m, a) => new Recipe().Rod(-0.15f, 0.4f, 0.04f, a).Post(-0.12f, 0.12f, 0.15f, m, 0, 0.45f)
                .DiscY(0.155f, 0.02f, 0, 0.12f, 0.45f, Blue).DiscY(0.155f, 0.02f, 0, -0.12f, 0.45f, Blue));
        W("Fish Slapper", S, WeaponEffect.Silly, H("5DADE2"), H("1B4F72"), "A cold, wet, floppy fish. Slap!",
            (m, a) => new Recipe().Box(0.12f, 0.01f, 0.08f, 0, 0, 0.03f, a, 0, 0, 90).Egg(0.07f, 0.14f, 0.5f, 0, 0, 0.32f, m)
                .Ball(0.03f, 0.03f, 0.03f, 0.5f, White).Ball(0.015f, 0.04f, 0.03f, 0.51f, Black),
            d => { d.knockback = 5f; });
        W("Pillow", S, WeaponEffect.Bounce, White, Pink, "PILLOW FIGHT! Soft hits, big bounce.",
            (m, a) => new Recipe().Egg(0.45f, 0.14f, 0.55f, 0, 0, 0.28f, m).Ball(0.05f, 0.2f, 0, 0.03f, a).Ball(0.05f, -0.2f, 0, 0.03f, a)
                .Ball(0.05f, 0.2f, 0, 0.53f, a).Ball(0.05f, -0.2f, 0, 0.53f, a),
            d => { d.damage = 2f; d.knockback = 6f; d.range = 0.25f; d.mass = 0.4f; });
        W("Party Horn Blaster", Sh, WeaponEffect.Silly, Pink, Gold, "Toot toot! Shoots party blasts.",
            (m, a) => new Recipe().Rod(-0.05f, 0.1f, 0.04f, a).Rod(0.1f, 0.25f, 0.07f, m).Rod(0.25f, 0.33f, 0.11f, a).DiscZ(0.15f, 0.02f, 0.34f, m),
            d => { d.projectileSize = 0.1f; d.damage = 2f; d.cooldown = 0.4f; });
        W("Tickle Feather", S, WeaponEffect.Shock, Pink, White, "Tickle tickle! They laugh so hard they can't move.",
            (m, a) => new Recipe().Rod(-0.1f, 0.4f, 0.015f, a).Egg(0.1f, 0.01f, 0.45f, 0, 0, 0.38f, m).Egg(0.07f, 0.012f, 0.3f, 0, 0, 0.42f, H("FFB3DE")),
            d => { d.damage = 1f; d.knockback = 0.5f; d.mass = 0.2f; });
        W("Whoopee Bomb", T, WeaponEffect.Silly, Pink, Red, "Throw it. Everyone nearby is embarrassed.",
            (m, a) => new Recipe().Egg(0.18f, 0.06f, 0.16f, 0, 0, 0.08f, m).Rod(-0.03f, 0.01f, 0.03f, a),
            d => { d.areaRadius = 2f; d.singleUse = true; d.damage = 3f; });
        W("Spaghetti Whip", S, WeaponEffect.Sticky, H("F7E7A1"), H("A0522D"), "Saucy noodles that stick to your target.",
            (m, a) =>
            {
                var r = new Recipe().Rod(-0.14f, 0.12f, 0.03f, Metal);
                for (int i = 0; i < 6; i++)
                    r.Pill(0.12f + i * 0.12f, 0.26f + i * 0.12f, 0.02f, m, (i % 2 == 0 ? 0.02f : -0.02f), -i * 0.02f);
                return r.Ball(0.08f, 0, -0.12f, 0.86f, a);
            });
        W("Burrito Bat", S, WeaponEffect.Fire, Tan, Metal, "Extra spicy. Sets targets on fire.",
            (m, a) => new Recipe().Rod(-0.15f, 0.1f, 0.05f, a).Rod(0.1f, 0.6f, 0.13f, m)
                .DiscZ(0.115f, 0.01f, 0.605f, Red).DiscZ(0.08f, 0.012f, 0.607f, Leaf).DiscZ(0.05f, 0.014f, 0.609f, Coconut));
        W("Disco Ball Flail", S, WeaponEffect.Silly, Metal, Purple, "Groovy. Glowing. Bonking.",
            (m, a) =>
            {
                var r = new Recipe().Handle(a);
                for (int i = 0; i < 4; i++) r.Ball(0.04f, 0, 0, 0.18f + i * 0.07f, Metal);
                r.Ball(0.2f, 0, 0, 0.55f, m);
                Color[] dots = { Pink, Cyan, Yellow, Goop };
                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = Quaternion.Euler(i * 45f, i * 70f, 0) * Vector3.forward * 0.1f;
                    r.Box(0.03f, 0.03f, 0.03f, p.x, p.y, 0.55f + p.z, dots[i % 4]);
                }
                return r;
            },
            d => { d.glows = true; d.range = 0.22f; });
    }
}
