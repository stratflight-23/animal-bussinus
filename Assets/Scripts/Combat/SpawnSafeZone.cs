using System.Collections.Generic;
using UnityEngine;

// SPAWN SAFE ZONE - a circle around the spawn point where players can't be hurt by ANYTHING.
// Add it with Gorilla Company > 8. Add Spawn Safe Zone.
//
// How it works: every kind of damage in the game (weapons, kittens, burning, lava...) goes through
// Health.TakeDamage / Health.Kill, and those ask SpawnSafeZone.Protects() first. So there's one check
// in one place, and nothing can sneak around it. Kittens also ignore players who are inside.
//
// MULTIPLAYER NOTE: right now each player's own game decides if they're in the zone. When multiplayer is
// added, the SERVER must decide who is inside the safe zone and block damage there (and refuse damage
// requests against those players). Never trust a player's own game for this, or hackers could fake it.
[DisallowMultipleComponent]
public class SpawnSafeZone : MonoBehaviour
{
    [Tooltip("Size of the safe circle in meters. 10 feet = 3.05 m.")]
    public float radius = 3.05f;
    [Tooltip("How high above the floor the zone still protects you (meters), so jumping inside is safe too.")]
    public float height = 4f;
    [Tooltip("Show the glowing ring and faint dome while playing.")]
    public bool showVisuals = true;
    public Color glowColor = new Color(0.3f, 1f, 0.45f);

    static readonly List<SpawnSafeZone> zones = new List<SpawnSafeZone>();

    Transform visuals;
    float builtRadius;
    PrimitiveColor[] ringParts;

    void OnEnable() => zones.Add(this);
    void OnDisable() => zones.Remove(this);

    // ------------------------------------------------------------ who is safe?

    public bool ContainsPoint(Vector3 p)
    {
        Vector3 d = p - transform.position;
        if (d.y < -1f || d.y > height) return false;
        d.y = 0f;
        return d.sqrMagnitude <= radius * radius;
    }

    // Is this spot inside any safe zone?
    public static bool IsInside(Vector3 position)
    {
        foreach (var z in zones)
            if (z != null && z.isActiveAndEnabled && z.ContainsPoint(position)) return true;
        return false;
    }

    // Players only (things with PlayerDeathPhysics). Training dummies etc. can still be hit in the zone.
    public static bool IsPlayer(Health h) => h != null && h.GetComponent<PlayerDeathPhysics>() != null;

    // True = this Health must not take damage right now.
    public static bool Protects(Health h) => IsPlayer(h) && IsInside(h.transform.position);

    // ------------------------------------------------------------ visuals (built when the game starts)

    void Start()
    {
        if (showVisuals) BuildVisuals();
    }

    void Update()
    {
        if (!showVisuals || visuals == null) return;
        if (!Mathf.Approximately(builtRadius, radius)) BuildVisuals(); // radius changed in the Inspector while playing

        // very gentle glow "breathing" (slow, no flashing)
        float a = 0.45f + 0.12f * Mathf.Sin(Time.time * 1.5f);
        Color c = new Color(glowColor.r, glowColor.g, glowColor.b, a);
        foreach (var p in ringParts) if (p != null) p.SetColor(c);
    }

    void BuildVisuals()
    {
        if (visuals != null) Destroy(visuals.gameObject);
        visuals = new GameObject("Safe Zone Visuals").transform;
        visuals.SetParent(transform, false);
        builtRadius = radius;

        // Glowing ring on the floor at the edge, made of short flat pieces around the circle.
        const int pieces = 64;
        ringParts = new PrimitiveColor[pieces];
        float pieceLength = 2f * Mathf.PI * radius / pieces * 1.08f;
        Color ring = new Color(glowColor.r, glowColor.g, glowColor.b, 0.5f);
        for (int i = 0; i < pieces; i++)
        {
            float angle = i * 360f / pieces;
            var piece = SeeThrough.MakeShape(PrimitiveType.Cube, ring, "Ring", visuals);
            piece.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            piece.transform.localPosition = piece.transform.localRotation * new Vector3(0f, 0.03f, radius);
            piece.transform.localScale = new Vector3(pieceLength, 0.01f, 0.12f);
            ringParts[i] = piece.GetComponent<PrimitiveColor>();
        }

        // A very faint dome you can barely see (the bottom half is hidden under the floor).
        var dome = SeeThrough.MakeShape(PrimitiveType.Sphere, new Color(glowColor.r, glowColor.g, glowColor.b, 0.035f), "Dome", visuals);
        dome.transform.localScale = new Vector3(radius * 2f, Mathf.Min(radius, height) * 2f, radius * 2f);
    }

    // Shows the circle in the Scene view (editor only), so you can see where it is without pressing Play.
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.9f);
        Vector3 c = transform.position + Vector3.up * 0.03f;
        Vector3 last = c + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= 64; i++)
        {
            float a = i * Mathf.PI * 2f / 64f;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(last, next);
            last = next;
        }
    }
}
