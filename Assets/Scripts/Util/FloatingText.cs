using TMPro;
using UnityEngine;

// Text that floats up and fades away, like damage numbers ("25") or "FROZEN!".
public class FloatingText : MonoBehaviour
{
    TextMeshPro tmp;
    Color baseColor;
    Vector3 start;
    float age;
    float life = 1.1f;

    public static void Spawn(string text, Vector3 position, Color color, float size = 1.2f)
    {
        var go = new GameObject("Floating Text");
        go.transform.position = position;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.5f);

        go.AddComponent<FaceCamera>();
        var ft = go.AddComponent<FloatingText>();
        ft.tmp = tmp;
        ft.baseColor = color;
        ft.start = position;
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / life;
        transform.position = start + Vector3.up * (0.5f * k);
        var c = baseColor;
        c.a = 1f - k * k;
        tmp.color = c;
        if (k >= 1f) Destroy(gameObject);
    }
}
