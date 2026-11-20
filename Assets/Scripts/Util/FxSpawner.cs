using UnityEngine;

// Simple cartoon visual effects made from spheres and cubes: smoke clouds, confetti, explosions, sparks.
public static class FxSpawner
{
    public static readonly Color GoopGreen = new Color(0.24f, 0.86f, 0.35f);
    public static readonly Color Ice = new Color(0.62f, 0.91f, 1f);
    public static readonly Color FireOrange = new Color(1f, 0.55f, 0.1f);
    public static readonly Color Spark = new Color(1f, 0.95f, 0.3f);
    public static readonly Color HealPink = new Color(1f, 0.5f, 0.75f);
    public static readonly Color ShieldBlue = new Color(0.4f, 0.75f, 1f);

    // Little bits flying outward.
    public static void Burst(Vector3 pos, Color color, int count = 8, float speed = 3f, float size = 0.07f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 v = Random.onUnitSphere * speed;
            v.y = Mathf.Abs(v.y) + 1f;
            Puff.Create(PrimitiveType.Sphere, pos, color, size, size * 0.2f, 0.5f, v, true);
        }
    }

    public static void Explosion(Vector3 pos, float radius, Color color)
    {
        Puff.Create(PrimitiveType.Sphere, pos, FireOrange, 0.2f, radius * 1.6f, 0.35f, Vector3.zero, false);
        Puff.Create(PrimitiveType.Sphere, pos, color, 0.2f, radius * 1.1f, 0.45f, Vector3.zero, false);
        Burst(pos, color, 14, 5f, 0.12f);
    }

    // A big cloud you can't see through for a few seconds.
    public static void Smoke(Vector3 pos, float radius = 1.3f)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector3 p = pos + new Vector3(Random.Range(-radius, radius), Random.Range(0f, radius), Random.Range(-radius, radius));
            float g = Random.Range(0.55f, 0.8f);
            var puff = Puff.Create(PrimitiveType.Sphere, p, new Color(g, g, g), 0.2f, Random.Range(1.0f, 1.6f), 5f,
                                   Vector3.up * 0.1f, false);
            puff.holdFraction = 0.75f;
        }
        SoundFx.Play(Sfx.Poof, pos);
    }

    public static void Confetti(Vector3 pos, int count = 30)
    {
        for (int i = 0; i < count; i++)
        {
            Color c = Color.HSVToRGB(Random.value, 0.8f, 1f);
            Vector3 v = Random.onUnitSphere * Random.Range(1.5f, 4f);
            v.y = Mathf.Abs(v.y) + 2f;
            var p = Puff.Create(PrimitiveType.Cube, pos, c, 0.05f, 0.05f, 1.6f, v, true);
            p.spin = Random.onUnitSphere * 720f;
            p.transform.localScale = new Vector3(0.06f, 0.01f, 0.04f);
            p.keepShape = true;
        }
    }

    // An expanding ring on the ground (for area attacks).
    public static void Ring(Vector3 pos, float radius, Color color)
    {
        var p = Puff.Create(PrimitiveType.Cylinder, pos, color, 0.2f, radius * 2f, 0.4f, Vector3.zero, false);
        p.flat = true;
    }

    public static void Sparks(Vector3 pos) => Burst(pos, Spark, 10, 4f, 0.05f);
}
