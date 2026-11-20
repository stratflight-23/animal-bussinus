using UnityEngine;

// When a kitten scratches the player wearing the headset, a few small scratch marks appear
// near the EDGE of their view, then gently shrink away.
// VR comfort first: no flashing, no shaking, nothing in the middle of your view.
// Added to the camera automatically - you don't need to set anything up.
public class ScratchVision : MonoBehaviour
{
    const float GrowSeconds = 0.12f;
    const float HoldSeconds = 0.5f;
    const float FadeSeconds = 0.4f;

    // Spots around the edge of the view (in front of the camera, in meters).
    static readonly Vector3[] Spots =
    {
        new Vector3(-0.27f, 0.14f, 0.45f),
        new Vector3(0.28f, -0.13f, 0.45f),
        new Vector3(0.26f, 0.15f, 0.45f),
        new Vector3(-0.28f, -0.12f, 0.45f),
    };

    static ScratchVision instance;

    Transform[] marks;
    float[] shownAt;
    int next;

    public static void Show()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (instance == null || instance.gameObject != cam.gameObject)
        {
            instance = cam.GetComponent<ScratchVision>();
            if (instance == null) instance = cam.gameObject.AddComponent<ScratchVision>();
        }
        instance.ShowNext();
    }

    void Awake()
    {
        marks = new Transform[Spots.Length];
        shownAt = new float[Spots.Length];
        Color soft = new Color(1f, 0.88f, 0.92f);
        for (int i = 0; i < Spots.Length; i++)
        {
            var mark = new GameObject("Scratch Mark").transform;
            mark.SetParent(transform, false);
            mark.localPosition = Spots[i];
            mark.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 30f : -30f) + i * 7f);
            for (int s = 0; s < 3; s++)
            {
                var line = KittyLook.Part(mark, PrimitiveType.Cube, "Line",
                                          new Vector3((s - 1) * 0.014f, (s - 1) * 0.006f, 0f),
                                          new Vector3(0.005f, 0.06f, 0.001f), Vector3.zero, soft);
                KittyLook.MakeGlow(line, soft);
            }
            mark.localScale = Vector3.zero;
            mark.gameObject.SetActive(false);
            marks[i] = mark;
            shownAt[i] = -100f;
        }
    }

    void ShowNext()
    {
        shownAt[next] = Time.time;
        marks[next].gameObject.SetActive(true);
        next = (next + 1) % marks.Length;
    }

    void Update()
    {
        for (int i = 0; i < marks.Length; i++)
        {
            if (!marks[i].gameObject.activeSelf) continue;
            float age = Time.time - shownAt[i];
            float s = age < GrowSeconds ? age / GrowSeconds
                    : age < GrowSeconds + HoldSeconds ? 1f
                    : 1f - (age - GrowSeconds - HoldSeconds) / FadeSeconds;
            if (s <= 0f)
            {
                marks[i].gameObject.SetActive(false);
                continue;
            }
            marks[i].localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, s);
        }
    }
}
