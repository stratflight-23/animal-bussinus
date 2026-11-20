using TMPro;
using UnityEngine;

// Helpers for building a panel of buttons you press by poking them with your VR hand
// (same style as the Mod Panel). The panel's front faces its -Z side (stand at -Z to use it).
public static class PokePanel
{
    public static readonly Color Grey = new Color(0.35f, 0.35f, 0.4f);
    public static readonly Color Green = new Color(0.2f, 0.7f, 0.3f);
    public static readonly Color Red = new Color(0.75f, 0.2f, 0.2f);
    public static readonly Color Blue = new Color(0.2f, 0.45f, 0.85f);
    public static readonly Color Pink = new Color(0.85f, 0.4f, 0.7f);
    public static readonly Color Gold = new Color(0.9f, 0.7f, 0.15f);

    public static GameObject Backing(Transform panel, Vector2 size, Color color)
    {
        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "Backing";
        back.transform.SetParent(panel, false);
        back.transform.localPosition = new Vector3(0f, 0f, 0.03f);
        back.transform.localScale = new Vector3(size.x, size.y, 0.02f);
        back.AddComponent<PrimitiveColor>().SetColor(color);
        return back;
    }

    public static GameObject Button(Transform panel, string label, Vector3 localPos, Vector2 size, Color color, System.Action onPress)
    {
        var root = new GameObject("Button " + label);
        root.transform.SetParent(panel, false);
        root.transform.localPosition = localPos;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Press";
        cube.transform.SetParent(root.transform, false);
        cube.transform.localScale = new Vector3(size.x, size.y, 0.03f);
        cube.GetComponent<Collider>().isTrigger = true;
        cube.GetComponent<Renderer>().material.color = color;
        cube.AddComponent<PokeButton>().onPressed = onPress;

        var text = Text(root.transform, "Label", new Vector3(0f, 0f, -0.02f), 0.26f, size);
        text.text = label;
        return root;
    }

    public static TextMeshPro Text(Transform parent, string objName, Vector3 localPos, float fontSize, Vector2 size)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.rectTransform.sizeDelta = size;
        tmp.text = "";
        return tmp;
    }

    // Makes sure both VR hands can press buttons.
    public static void EnsureHandPokers()
    {
        foreach (var handName in new[] { "Left Hand", "Right Hand" })
        {
            var hand = GameObject.Find(handName);
            if (hand != null && hand.GetComponent<HandPoker>() == null) hand.AddComponent<HandPoker>();
        }
    }

    // For flat-screen testing: true if the player's head is within "range" meters of this spot.
    public static bool PlayerNear(Vector3 position, float range)
    {
        var cam = Camera.main;
        return cam != null && (cam.transform.position - position).sqrMagnitude < range * range;
    }
}
