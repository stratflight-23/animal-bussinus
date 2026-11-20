using System.Collections.Generic;
using UnityEngine;

// The 10 original pets, built from simple shapes. Gorilla Company > 12. Generate Pets turns these into files in
// Assets/Pets/Data. Coordinates: feet on the ground at y = 0, nose toward +Z, sizes in meters.
// Shapes named "Tail" wag around the tail pivot; shapes named "Wing" flap.
public static class PetCatalog
{
    public class Def
    {
        public string id, name, desc;
        public List<PartRecipe> parts = new List<PartRecipe>();
        public float size = 1f, speed = 3f;
        public bool flies, bouncy;
        public PetTrick trick;
        public Vector3 tailPivot = new Vector3(0f, 0.15f, -0.15f);
    }

    static readonly Color Black = new Color(0.08f, 0.08f, 0.1f);
    static readonly Color White = new Color(0.97f, 0.97f, 0.97f);
    static readonly Color Pink = new Color(1f, 0.5f, 0.65f);

    static PartRecipe S(float x, float y, float z, Vector3 s, Color c, Vector3 r = default, string n = null, bool glow = false) => new PartRecipe(PrimitiveType.Sphere, s, new Vector3(x, y, z), c, r, n, glow);
    static PartRecipe C(float x, float y, float z, Vector3 s, Color c, Vector3 r = default, string n = null, bool glow = false) => new PartRecipe(PrimitiveType.Cube, s, new Vector3(x, y, z), c, r, n, glow);
    static PartRecipe Y(float x, float y, float z, Vector3 s, Color c, Vector3 r = default, string n = null, bool glow = false) => new PartRecipe(PrimitiveType.Cylinder, s, new Vector3(x, y, z), c, r, n, glow);
    static PartRecipe P(float x, float y, float z, Vector3 s, Color c, Vector3 r = default, string n = null, bool glow = false) => new PartRecipe(PrimitiveType.Capsule, s, new Vector3(x, y, z), c, r, n, glow);
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Vector3 U(float s) => Vector3.one * s;

    // two cartoon eyes (white with a black dot) at height y, spread x, front z
    static IEnumerable<PartRecipe> Eyes(float x, float y, float z, float size = 0.045f)
    {
        yield return S(-x, y, z, U(size), White);
        yield return S(x, y, z, U(size), White);
        yield return S(-x, y, z + size * 0.35f, U(size * 0.5f), Black);
        yield return S(x, y, z + size * 0.35f, U(size * 0.5f), Black);
    }

    static Def Pet(List<Def> list, string id, string name, string desc, PetTrick trick, float speed)
    {
        var d = new Def { id = id, name = name, desc = desc, trick = trick, speed = speed };
        list.Add(d);
        return d;
    }

    public static List<Def> All()
    {
        var L = new List<Def>();

        // Goop Blob: a bouncy green slime
        Color goop = new Color(0.3f, 0.9f, 0.4f);
        var blob = Pet(L, "goop_blob", "Goop Blob", "A bouncy little slime made of goop.", PetTrick.Hop, 2.8f);
        blob.bouncy = true;
        blob.parts.Add(S(0f, 0.13f, 0f, V(0.34f, 0.26f, 0.34f), goop, default, null, true));
        blob.parts.Add(S(0.08f, 0.22f, -0.05f, U(0.07f), new Color(0.7f, 1f, 0.75f), default, null, true));
        blob.parts.AddRange(Eyes(0.06f, 0.17f, 0.15f, 0.06f));
        blob.parts.Add(S(0f, 0.1f, 0.165f, V(0.06f, 0.025f, 0.02f), new Color(0.1f, 0.4f, 0.15f)));

        // Mini Gorilla: a tiny buddy who looks like you
        Color fur = new Color(0.27f, 0.24f, 0.22f), face = new Color(0.55f, 0.47f, 0.4f);
        var gor = Pet(L, "mini_gorilla", "Mini Gorilla", "A tiny gorilla buddy. Copies your style.", PetTrick.Sit, 3.2f);
        gor.parts.Add(S(0f, 0.17f, 0f, V(0.24f, 0.24f, 0.2f), fur));
        gor.parts.Add(S(0f, 0.33f, 0.03f, U(0.17f), fur));
        gor.parts.Add(S(0f, 0.31f, 0.09f, V(0.12f, 0.1f, 0.07f), face));
        gor.parts.Add(S(0f, 0.15f, 0.07f, V(0.14f, 0.15f, 0.08f), face));
        gor.parts.AddRange(Eyes(0.035f, 0.345f, 0.1f, 0.035f));
        gor.parts.Add(P(-0.14f, 0.13f, 0.03f, V(0.06f, 0.1f, 0.06f), fur, V(0, 0, -12)));
        gor.parts.Add(P(0.14f, 0.13f, 0.03f, V(0.06f, 0.1f, 0.06f), fur, V(0, 0, 12)));
        gor.parts.Add(S(-0.06f, 0.03f, 0f, V(0.08f, 0.06f, 0.1f), fur));
        gor.parts.Add(S(0.06f, 0.03f, 0f, V(0.08f, 0.06f, 0.1f), fur));

        // Banana Pup: a yellow puppy with brown banana-tip ears
        Color banana = new Color(1f, 0.85f, 0.25f), tip = new Color(0.4f, 0.26f, 0.1f);
        var pup = Pet(L, "banana_pup", "Banana Pup", "A happy yellow pup. Loves wagging.", PetTrick.Wag, 3.6f);
        pup.tailPivot = V(0f, 0.2f, -0.15f);
        pup.parts.Add(P(0f, 0.17f, 0f, V(0.16f, 0.15f, 0.16f), banana, V(90, 0, 0)));
        pup.parts.Add(S(0f, 0.28f, 0.16f, U(0.16f), banana));
        pup.parts.Add(S(0f, 0.25f, 0.24f, V(0.08f, 0.06f, 0.08f), new Color(1f, 0.93f, 0.6f)));
        pup.parts.Add(S(0f, 0.27f, 0.28f, U(0.03f), Black));
        pup.parts.AddRange(Eyes(0.04f, 0.31f, 0.22f, 0.035f));
        pup.parts.Add(S(-0.08f, 0.3f, 0.13f, V(0.04f, 0.1f, 0.06f), tip, V(0, 0, 25)));
        pup.parts.Add(S(0.08f, 0.3f, 0.13f, V(0.04f, 0.1f, 0.06f), tip, V(0, 0, -25)));
        foreach (var lx in new[] { -0.05f, 0.05f })
            foreach (var lz in new[] { -0.08f, 0.08f })
                pup.parts.Add(Y(lx, 0.05f, lz, V(0.05f, 0.05f, 0.05f), banana));
        pup.parts.Add(P(0f, 0.27f, -0.19f, V(0.04f, 0.07f, 0.04f), banana, V(-35, 0, 0), "Tail"));
        pup.parts.Add(S(0f, 0.32f, -0.22f, U(0.035f), tip, default, "Tail"));

        // Coconut Crab: a coconut shell with orange claws
        Color coco = new Color(0.42f, 0.27f, 0.13f), crab = new Color(1f, 0.45f, 0.2f);
        var cr = Pet(L, "coconut_crab", "Coconut Crab", "Lives in a coconut. Snaps its claws to say hi.", PetTrick.Spin, 2.6f);
        cr.parts.Add(S(0f, 0.11f, 0f, V(0.26f, 0.17f, 0.24f), coco));
        cr.parts.Add(S(0f, 0.17f, -0.02f, V(0.14f, 0.06f, 0.12f), new Color(0.3f, 0.18f, 0.08f)));
        cr.parts.Add(S(-0.17f, 0.1f, 0.1f, V(0.08f, 0.06f, 0.08f), crab));
        cr.parts.Add(S(0.17f, 0.1f, 0.1f, V(0.08f, 0.06f, 0.08f), crab));
        cr.parts.Add(C(-0.17f, 0.12f, 0.15f, V(0.03f, 0.02f, 0.06f), crab, V(0, 20, 0)));
        cr.parts.Add(C(0.17f, 0.12f, 0.15f, V(0.03f, 0.02f, 0.06f), crab, V(0, -20, 0)));
        cr.parts.Add(Y(-0.04f, 0.22f, 0.08f, V(0.015f, 0.04f, 0.015f), crab));
        cr.parts.Add(Y(0.04f, 0.22f, 0.08f, V(0.015f, 0.04f, 0.015f), crab));
        cr.parts.AddRange(Eyes(0.04f, 0.27f, 0.08f, 0.035f));
        for (int i = 0; i < 3; i++)
        {
            float z = -0.06f + i * 0.06f;
            cr.parts.Add(Y(-0.14f, 0.04f, z, V(0.02f, 0.05f, 0.02f), crab, V(0, 0, 45)));
            cr.parts.Add(Y(0.14f, 0.04f, z, V(0.02f, 0.05f, 0.02f), crab, V(0, 0, -45)));
        }

        // Leaf Bunny: white bunny with leaf ears
        Color leaf = new Color(0.3f, 0.75f, 0.3f), cream = new Color(0.95f, 0.95f, 0.88f);
        var bun = Pet(L, "leaf_bunny", "Leaf Bunny", "A fluffy bunny whose ears are leaves.", PetTrick.Hop, 3.4f);
        bun.tailPivot = V(0f, 0.12f, -0.12f);
        bun.parts.Add(S(0f, 0.12f, 0f, V(0.2f, 0.2f, 0.24f), cream));
        bun.parts.Add(S(0f, 0.26f, 0.09f, U(0.15f), cream));
        bun.parts.AddRange(Eyes(0.04f, 0.28f, 0.15f, 0.035f));
        bun.parts.Add(S(0f, 0.25f, 0.165f, U(0.025f), Pink));
        bun.parts.Add(S(-0.035f, 0.42f, 0.06f, V(0.05f, 0.16f, 0.02f), leaf, V(-10, 0, 10)));
        bun.parts.Add(S(0.035f, 0.42f, 0.06f, V(0.05f, 0.16f, 0.02f), leaf, V(-10, 0, -10)));
        bun.parts.Add(S(-0.07f, 0.04f, 0.04f, V(0.06f, 0.05f, 0.12f), cream));
        bun.parts.Add(S(0.07f, 0.04f, 0.04f, V(0.06f, 0.05f, 0.12f), cream));
        bun.parts.Add(S(0f, 0.13f, -0.13f, U(0.07f), White, default, "Tail"));

        // Glow Bug: a flying firefly with a glowing tail light
        Color bug = new Color(0.2f, 0.2f, 0.25f), glowY = new Color(0.85f, 1f, 0.3f);
        var gb = Pet(L, "glow_bug", "Glow Bug", "A friendly firefly. Lights up dark temples.", PetTrick.Spin, 3.2f);
        gb.flies = true;
        gb.size = 0.9f;
        gb.parts.Add(S(0f, 0f, -0.06f, V(0.1f, 0.1f, 0.13f), glowY, default, null, true));
        gb.parts.Add(S(0f, 0.01f, 0.04f, V(0.09f, 0.09f, 0.1f), bug));
        gb.parts.Add(S(0f, 0.02f, 0.11f, U(0.08f), bug));
        gb.parts.AddRange(Eyes(0.025f, 0.035f, 0.14f, 0.03f));
        gb.parts.Add(Y(-0.02f, 0.08f, 0.13f, V(0.006f, 0.035f, 0.006f), bug, V(-20, 0, 15)));
        gb.parts.Add(Y(0.02f, 0.08f, 0.13f, V(0.006f, 0.035f, 0.006f), bug, V(-20, 0, -15)));
        gb.parts.Add(S(-0.07f, 0.06f, 0.02f, V(0.12f, 0.01f, 0.06f), new Color(0.85f, 0.95f, 1f), V(0, 0, 20), "Wing"));
        gb.parts.Add(S(0.07f, 0.06f, 0.02f, V(0.12f, 0.01f, 0.06f), new Color(0.85f, 0.95f, 1f), V(0, 0, -20), "Wing"));

        // Rock Turtle: a turtle with a stone shell
        Color stone = new Color(0.5f, 0.5f, 0.47f), skin = new Color(0.45f, 0.7f, 0.35f);
        var tur = Pet(L, "rock_turtle", "Rock Turtle", "Slow and steady, with a shell made of temple stone.", PetTrick.Sit, 2.2f);
        tur.tailPivot = V(0f, 0.06f, -0.15f);
        tur.parts.Add(S(0f, 0.1f, 0f, V(0.3f, 0.17f, 0.32f), stone));
        tur.parts.Add(C(-0.05f, 0.17f, 0.03f, V(0.07f, 0.03f, 0.07f), new Color(0.38f, 0.38f, 0.36f), V(0, 30, 0)));
        tur.parts.Add(C(0.06f, 0.16f, -0.06f, V(0.06f, 0.03f, 0.06f), new Color(0.38f, 0.38f, 0.36f), V(0, 10, 0)));
        tur.parts.Add(S(0f, 0.11f, 0.2f, V(0.1f, 0.09f, 0.11f), skin));
        tur.parts.AddRange(Eyes(0.035f, 0.13f, 0.24f, 0.03f));
        foreach (var lx in new[] { -0.12f, 0.12f })
            foreach (var lz in new[] { -0.1f, 0.1f })
                tur.parts.Add(S(lx, 0.035f, lz, V(0.07f, 0.06f, 0.07f), skin));
        tur.parts.Add(S(0f, 0.06f, -0.17f, V(0.04f, 0.03f, 0.06f), skin, default, "Tail"));

        // Baby Toucan: a flying bird with a big orange beak
        Color orange = new Color(1f, 0.55f, 0.1f);
        var tou = Pet(L, "baby_toucan", "Baby Toucan", "A chatty bird with a beak that's way too big.", PetTrick.Hop, 3.4f);
        tou.flies = true;
        tou.tailPivot = V(0f, 0f, -0.08f);
        tou.parts.Add(S(0f, 0f, 0f, V(0.14f, 0.15f, 0.16f), Black));
        tou.parts.Add(S(0f, 0f, 0.05f, V(0.1f, 0.1f, 0.08f), White));
        tou.parts.Add(S(0f, 0.08f, 0.04f, U(0.1f), Black));
        tou.parts.Add(P(0f, 0.07f, 0.13f, V(0.045f, 0.06f, 0.05f), orange, V(80, 0, 0)));
        tou.parts.Add(S(0f, 0.075f, 0.18f, U(0.03f), Black));
        tou.parts.AddRange(Eyes(0.035f, 0.1f, 0.07f, 0.03f));
        tou.parts.Add(S(-0.08f, 0.01f, -0.01f, V(0.12f, 0.02f, 0.09f), Black, V(0, 0, 15), "Wing"));
        tou.parts.Add(S(0.08f, 0.01f, -0.01f, V(0.12f, 0.02f, 0.09f), Black, V(0, 0, -15), "Wing"));
        tou.parts.Add(C(0f, -0.02f, -0.11f, V(0.06f, 0.015f, 0.08f), Black, V(-15, 0, 0), "Tail"));

        // Crystal Fox: an orange fox with a glowing crystal tail
        Color fox = new Color(0.95f, 0.5f, 0.2f), crystal = new Color(0.4f, 0.9f, 1f);
        var fx = Pet(L, "crystal_fox", "Crystal Fox", "A clever fox. Its tail is made of glowing crystals.", PetTrick.Spin, 3.8f);
        fx.tailPivot = V(0f, 0.17f, -0.14f);
        fx.parts.Add(P(0f, 0.16f, 0f, V(0.14f, 0.14f, 0.14f), fox, V(90, 0, 0)));
        fx.parts.Add(S(0f, 0.26f, 0.15f, U(0.14f), fox));
        fx.parts.Add(S(0f, 0.23f, 0.23f, V(0.07f, 0.06f, 0.09f), White));
        fx.parts.Add(S(0f, 0.24f, 0.275f, U(0.025f), Black));
        fx.parts.AddRange(Eyes(0.035f, 0.29f, 0.2f, 0.032f));
        fx.parts.Add(C(-0.045f, 0.35f, 0.13f, V(0.05f, 0.05f, 0.015f), fox, V(0, 0, 45)));
        fx.parts.Add(C(0.045f, 0.35f, 0.13f, V(0.05f, 0.05f, 0.015f), fox, V(0, 0, 45)));
        foreach (var lx in new[] { -0.045f, 0.045f })
            foreach (var lz in new[] { -0.08f, 0.08f })
                fx.parts.Add(Y(lx, 0.05f, lz, V(0.04f, 0.05f, 0.04f), Black));
        fx.parts.Add(C(0f, 0.22f, -0.2f, V(0.06f, 0.06f, 0.12f), crystal, V(30, 0, 45), "Tail", true));
        fx.parts.Add(C(0f, 0.27f, -0.27f, V(0.05f, 0.05f, 0.09f), crystal, V(45, 0, 45), "Tail", true));
        fx.parts.Add(C(0f, 0.31f, -0.31f, V(0.035f, 0.035f, 0.06f), White, V(55, 0, 45), "Tail", true));

        // Robo Monkey: a little metal monkey with glowing eyes
        Color metal = new Color(0.62f, 0.66f, 0.72f), dark = new Color(0.35f, 0.38f, 0.42f), eye = new Color(0.3f, 1f, 1f);
        var rm = Pet(L, "robo_monkey", "Robo Monkey", "Beep boop! A tin monkey with a springy tail.", PetTrick.Wag, 3.4f);
        rm.tailPivot = V(0f, 0.14f, -0.08f);
        rm.parts.Add(C(0f, 0.16f, 0f, V(0.16f, 0.17f, 0.13f), metal));
        rm.parts.Add(C(0f, 0.32f, 0.02f, V(0.15f, 0.13f, 0.13f), metal));
        rm.parts.Add(C(0f, 0.3f, 0.085f, V(0.11f, 0.06f, 0.01f), dark));
        rm.parts.Add(C(-0.035f, 0.33f, 0.09f, V(0.03f, 0.02f, 0.01f), eye, default, null, true));
        rm.parts.Add(C(0.035f, 0.33f, 0.09f, V(0.03f, 0.02f, 0.01f), eye, default, null, true));
        rm.parts.Add(Y(-0.095f, 0.33f, 0.02f, V(0.06f, 0.008f, 0.06f), dark, V(0, 0, 90)));
        rm.parts.Add(Y(0.095f, 0.33f, 0.02f, V(0.06f, 0.008f, 0.06f), dark, V(0, 0, 90)));
        rm.parts.Add(Y(0f, 0.42f, 0.02f, V(0.01f, 0.03f, 0.01f), dark));
        rm.parts.Add(S(0f, 0.455f, 0.02f, U(0.025f), new Color(1f, 0.3f, 0.3f), default, null, true));
        rm.parts.Add(Y(-0.11f, 0.15f, 0f, V(0.03f, 0.07f, 0.03f), dark, V(0, 0, -10)));
        rm.parts.Add(Y(0.11f, 0.15f, 0f, V(0.03f, 0.07f, 0.03f), dark, V(0, 0, 10)));
        rm.parts.Add(C(-0.045f, 0.035f, 0f, V(0.05f, 0.07f, 0.06f), dark));
        rm.parts.Add(C(0.045f, 0.035f, 0f, V(0.05f, 0.07f, 0.06f), dark));
        rm.parts.Add(Y(0f, 0.17f, -0.12f, V(0.02f, 0.05f, 0.02f), dark, V(-60, 0, 0), "Tail"));
        rm.parts.Add(Y(0f, 0.24f, -0.17f, V(0.02f, 0.05f, 0.02f), dark, V(-10, 0, 0), "Tail"));
        rm.parts.Add(S(0f, 0.29f, -0.16f, U(0.035f), metal, default, "Tail"));
        return L;
    }
}
