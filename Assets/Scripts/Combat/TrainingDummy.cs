using TMPro;
using UnityEngine;

// A target to practice on. Shows damage numbers, wobbles when hit, falls over at 0 HP
// and pops back up after a few seconds. Make one with Gorilla Company > Extras > Add Training Dummy.
[RequireComponent(typeof(Health))]
public class TrainingDummy : MonoBehaviour
{
    public float respawnSeconds = 3f;

    Health health;
    Transform visual;
    TextMeshPro hpText;
    Collider bodyCollider;
    Vector3 homePosition;
    Quaternion homeRotation;

    float wobble;
    Vector3 wobbleAxis = Vector3.right;
    float deadTimer;
    float fallAmount;

    void Awake()
    {
        health = GetComponent<Health>();
        visual = transform.Find("Visual");
        hpText = GetComponentInChildren<TextMeshPro>();
        bodyCollider = GetComponent<Collider>();
        homePosition = transform.position;
        homeRotation = transform.rotation;

        health.Damaged += OnDamaged;
        health.Healed += amount => FloatingText.Spawn("+" + Mathf.RoundToInt(amount), TopPoint, new Color(0.3f, 1f, 0.4f));
        health.Blocked += () => FloatingText.Spawn("BLOCKED", TopPoint, new Color(0.5f, 0.8f, 1f));
        health.Died += OnDied;
    }

    Vector3 TopPoint => transform.position + Vector3.up * 2.0f + Random.insideUnitSphere * 0.15f;

    void OnDamaged(float amount, Vector3 hitPoint)
    {
        int shown = Mathf.Max(1, Mathf.RoundToInt(amount));
        Color c = amount >= 25f ? new Color(1f, 0.3f, 0.2f) : amount >= 12f ? new Color(1f, 0.6f, 0.1f) : new Color(1f, 0.95f, 0.3f);
        FloatingText.Spawn(shown.ToString(), TopPoint, c, amount >= 25f ? 1.6f : 1.2f);

        Vector3 away = transform.position - hitPoint;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
        wobbleAxis = Vector3.Cross(Vector3.up, away.normalized);
        wobble = Mathf.Min(1f, wobble + Mathf.Clamp(amount / 20f, 0.3f, 1f));
    }

    void OnDied()
    {
        deadTimer = respawnSeconds;
        if (bodyCollider != null) bodyCollider.enabled = false;
        FloatingText.Spawn("KO!", transform.position + Vector3.up * 2.2f, new Color(1f, 0.3f, 0.8f), 2f);
    }

    void Update()
    {
        if (hpText != null)
            hpText.text = health.IsDead ? "KO" : Mathf.CeilToInt(health.CurrentHealth) + " HP";

        if (visual == null) return;

        if (health.IsDead)
        {
            fallAmount = Mathf.MoveTowards(fallAmount, 1f, Time.deltaTime * 3f);
            deadTimer -= Time.deltaTime;
            if (deadTimer <= 0f) Respawn();
        }
        else
        {
            fallAmount = Mathf.MoveTowards(fallAmount, 0f, Time.deltaTime * 4f);
        }

        wobble = Mathf.MoveTowards(wobble, 0f, Time.deltaTime * 1.5f);
        Vector3 localAxis = transform.InverseTransformDirection(wobbleAxis);
        float wobbleAngle = Mathf.Sin(Time.time * 22f) * wobble * 18f;
        visual.localRotation = Quaternion.AngleAxis(wobbleAngle + fallAmount * 80f, localAxis);
    }

    void Respawn()
    {
        transform.SetPositionAndRotation(homePosition, homeRotation);
        health.Revive();
        if (bodyCollider != null) bodyCollider.enabled = true;
        FloatingText.Spawn("READY", transform.position + Vector3.up * 2.2f, Color.white);
    }

    // ------------------------------------------------------------ building one

    // Builds a dummy out of simple shapes. Works from editor menus and while playing.
    public static GameObject Create(Vector3 position, string name = "Training Dummy")
    {
        var root = new GameObject(name);
        root.transform.position = position;

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.95f, 0f);
        col.height = 1.9f;
        col.radius = 0.32f;

        var visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);

        Color sack = new Color(0.85f, 0.72f, 0.48f);
        Color wood = new Color(0.45f, 0.3f, 0.16f);
        Color red = new Color(0.9f, 0.2f, 0.2f);

        Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.7f, 0.04f, 0.7f), Vector3.zero, wood);       // base
        Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.1f, 0.45f, 0.1f), Vector3.zero, wood);       // pole
        Part(visual, PrimitiveType.Capsule, new Vector3(0f, 1.2f, 0f), new Vector3(0.55f, 0.45f, 0.45f), Vector3.zero, sack);       // body
        Part(visual, PrimitiveType.Sphere, new Vector3(0f, 1.78f, 0f), new Vector3(0.36f, 0.36f, 0.36f), Vector3.zero, sack);       // head
        Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 1.35f, 0f), new Vector3(0.08f, 0.55f, 0.08f), new Vector3(0, 0, 90), wood); // arms
        Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0.22f), new Vector3(0.34f, 0.01f, 0.34f), new Vector3(90, 0, 0), Color.white); // target
        Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0.23f), new Vector3(0.22f, 0.01f, 0.22f), new Vector3(90, 0, 0), red);
        Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0.24f), new Vector3(0.09f, 0.01f, 0.09f), new Vector3(90, 0, 0), Color.white);
        Part(visual, PrimitiveType.Sphere, new Vector3(-0.08f, 1.82f, 0.16f), Vector3.one * 0.07f, Vector3.zero, Color.black);     // eyes
        Part(visual, PrimitiveType.Sphere, new Vector3(0.08f, 1.82f, 0.16f), Vector3.one * 0.07f, Vector3.zero, Color.black);

        var label = new GameObject("HP Label");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0f, 2.15f, 0f);
        var tmp = label.AddComponent<TextMeshPro>();
        tmp.text = "HP";
        tmp.fontSize = 1f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(1.2f, 0.3f);
        label.AddComponent<FaceCamera>();

        var health = root.AddComponent<Health>();
        health.maxHealth = 200f;
        root.AddComponent<StatusEffects>();
        root.AddComponent<TrainingDummy>();
        return root;
    }

    static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 euler, Color color)
    {
        var go = PrimitiveColor.MakeShape(type, color, type.ToString());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
    }
}
