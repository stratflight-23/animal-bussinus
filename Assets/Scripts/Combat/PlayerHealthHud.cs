using TMPro;
using UnityEngine;

// The player's health display. Added to GorillaPlayer automatically when the game starts.
//   - LEFT WRIST: a small health bar (green above 50, yellow 25-50, red below 25), the number, and
//     "SAFE ZONE" while you're inside the spawn safe zone. It turns to face your eyes, so just look at your wrist.
//   - Taking damage: a soft red glow fades in and out at the EDGES of your view.
//     VR comfort first: no camera shake, no hard flashing, nothing in the middle of your view.
[RequireComponent(typeof(Health))]
public class PlayerHealthHud : MonoBehaviour
{
    [Tooltip("Where the display sits, relative to the left controller (meters). -Z = toward your wrist.")]
    public Vector3 wristOffset = new Vector3(0f, 0.045f, -0.09f);

    const float BarWidth = 0.08f;
    static readonly Color Green = new Color(0.25f, 0.9f, 0.35f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.2f);
    static readonly Color Red = new Color(1f, 0.25f, 0.2f);
    static readonly Color SafeGreen = new Color(0.45f, 1f, 0.55f);

    // red damage glow
    const float FlashIn = 0.08f;
    const float FlashOut = 0.45f;

    Health health;
    Transform wrist;
    Transform fill;
    PrimitiveColor fillColor;
    TextMeshPro numberText, safeText;
    int shownNumber = -1;

    PrimitiveColor[] flashParts;
    float flashAt = -10f;
    float flashStrength;

    void Start()
    {
        health = GetComponent<Health>();
        health.Damaged += OnDamaged;
        BuildWrist();
        BuildFlash();
        Refresh();
    }

    void OnDestroy()
    {
        if (health != null) health.Damaged -= OnDamaged;
    }

    void OnDamaged(float amount, Vector3 point)
    {
        flashAt = Time.time;
        flashStrength = Mathf.Clamp(amount / 30f, 0.18f, 0.4f); // bigger hits = a bit stronger, never harsh
    }

    void Update()
    {
        Refresh();
        UpdateFlash();
    }

    // ------------------------------------------------------------ wrist display

    void Refresh()
    {
        if (wrist == null) return;
        float max = Mathf.Max(1f, health.maxHealth);
        float hp = health.IsDead ? 0f : health.CurrentHealth;
        float f = Mathf.Clamp01(hp / max);
        float percent = f * 100f;

        fill.localScale = new Vector3(Mathf.Max(0.0001f, BarWidth * f), 0.012f, 0.003f);
        fill.localPosition = new Vector3(-BarWidth / 2f + BarWidth * f / 2f, -0.006f, -0.001f);
        fillColor.SetColor(percent > 50f ? Green : percent >= 25f ? Yellow : Red);

        int number = Mathf.CeilToInt(hp);
        if (number != shownNumber)
        {
            shownNumber = number;
            numberText.text = number.ToString();
        }
        bool safe = SpawnSafeZone.Protects(health);
        if (safeText.gameObject.activeSelf != safe) safeText.gameObject.SetActive(safe);
    }

    void BuildWrist()
    {
        var hand = FindChild(transform, "Left Hand");
        if (hand == null) return;

        // The hand sphere is scaled down (0.1), so undo that for the display.
        float handScale = Mathf.Max(0.0001f, hand.lossyScale.x);
        var holder = new GameObject("Wrist Health").transform;
        holder.SetParent(hand, false);
        holder.localScale = Vector3.one / handScale;
        holder.localPosition = wristOffset / handScale;

        wrist = new GameObject("Panel").transform;
        wrist.SetParent(holder, false);
        wrist.gameObject.AddComponent<FaceCamera>().yawOnly = false;

        var back = PrimitiveColor.MakeShape(PrimitiveType.Cube, new Color(0.08f, 0.08f, 0.1f), "Backing");
        Place(back.transform, new Vector3(0f, 0f, 0.003f), new Vector3(0.1f, 0.062f, 0.003f));
        var barBack = PrimitiveColor.MakeShape(PrimitiveType.Cube, new Color(0.25f, 0.25f, 0.28f), "Bar Back");
        Place(barBack.transform, new Vector3(0f, -0.006f, 0f), new Vector3(BarWidth, 0.012f, 0.002f));
        var fillGo = PrimitiveColor.MakeShape(PrimitiveType.Cube, Green, "Bar Fill");
        fill = fillGo.transform;
        Place(fill, Vector3.zero, Vector3.one * 0.01f);
        fillColor = fillGo.GetComponent<PrimitiveColor>();

        numberText = MakeText("Number", new Vector3(0f, 0.014f, -0.003f), 0.2f, Color.white);
        safeText = MakeText("Safe Zone", new Vector3(0f, -0.022f, -0.003f), 0.12f, SafeGreen);
        safeText.text = "SAFE ZONE";
        safeText.fontStyle = FontStyles.Bold;

        foreach (var r in wrist.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void Place(Transform t, Vector3 localPos, Vector3 scale)
    {
        t.SetParent(wrist, false);
        t.localPosition = localPos;
        t.localScale = scale;
    }

    TextMeshPro MakeText(string objName, Vector3 localPos, float size, Color color)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(wrist, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(0.1f, 0.02f);
        return tmp;
    }

    static Transform FindChild(Transform root, string childName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == childName) return t;
        return null;
    }

    // ------------------------------------------------------------ red glow at the edges of the view

    void BuildFlash()
    {
        var cam = Camera.main;
        if (cam == null || !cam.transform.IsChildOf(transform)) return; // only for the player wearing the headset

        var root = new GameObject("Damage Glow").transform;
        root.SetParent(cam.transform, false);

        // four soft strips around the edge of the view (left, right, top, bottom), 0.3 m in front of the eyes
        Vector3[] pos = { new Vector3(-0.2f, 0f, 0.3f), new Vector3(0.2f, 0f, 0.3f), new Vector3(0f, 0.15f, 0.3f), new Vector3(0f, -0.15f, 0.3f) };
        Vector3[] size = { new Vector3(0.12f, 0.5f, 0.001f), new Vector3(0.12f, 0.5f, 0.001f), new Vector3(0.5f, 0.09f, 0.001f), new Vector3(0.5f, 0.09f, 0.001f) };
        flashParts = new PrimitiveColor[pos.Length];
        for (int i = 0; i < pos.Length; i++)
        {
            var strip = SeeThrough.MakeShape(PrimitiveType.Cube, new Color(1f, 0f, 0f, 0f), "Glow", root);
            strip.transform.localPosition = pos[i];
            strip.transform.localScale = size[i];
            strip.SetActive(false);
            flashParts[i] = strip.GetComponent<PrimitiveColor>();
        }
    }

    void UpdateFlash()
    {
        if (flashParts == null) return;
        float age = Time.time - flashAt;
        float a = age < FlashIn ? age / FlashIn
                : age < FlashIn + FlashOut ? 1f - (age - FlashIn) / FlashOut
                : 0f;
        a = Mathf.SmoothStep(0f, 1f, a) * flashStrength;

        bool on = a > 0.001f;
        foreach (var p in flashParts)
        {
            if (p.gameObject.activeSelf != on) p.gameObject.SetActive(on);
            if (on) p.SetColor(new Color(0.9f, 0.05f, 0.05f, a));
        }
    }
}
