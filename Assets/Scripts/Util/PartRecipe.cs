using System;
using System.Collections.Generic;
using UnityEngine;

// One shape in a cosmetic or pet "recipe": a cube/sphere/capsule/cylinder with a size, position, rotation and color.
// Optional: glow (always bright), spin (turns around its own up axis, like a propeller),
// and a name - pets use the names "Tail" (wags) and "Wing" (flaps).
[Serializable]
public class PartRecipe
{
    public string name = "Part";
    public PrimitiveType shape = PrimitiveType.Sphere;
    [Tooltip("The shape's scale. Cylinders and capsules are 2 units tall at scale 1.")]
    public Vector3 size = Vector3.one * 0.1f;
    public Vector3 offset;
    public Vector3 rotation;
    public Color color = Color.white;
    public bool glow;
    [Tooltip("Degrees per second around its own up axis (0 = no spinning).")]
    public float spin;

    public PartRecipe() { }

    public PartRecipe(PrimitiveType shape, Vector3 size, Vector3 offset, Color color, Vector3 rotation = default,
                      string name = null, bool glow = false, float spin = 0f)
    {
        this.shape = shape;
        this.size = size;
        this.offset = offset;
        this.color = color;
        this.rotation = rotation;
        this.name = name ?? shape.ToString();
        this.glow = glow;
        this.spin = spin;
    }

    // Builds all the parts under "parent" (no colliders). layer < 0 = keep the default layer.
    public static List<GameObject> Build(IList<PartRecipe> parts, Transform parent, int layer = -1)
    {
        var made = new List<GameObject>();
        if (parts == null) return made;
        foreach (var p in parts)
        {
            if (p == null) continue;
            var go = PrimitiveColor.MakeShape(p.shape, p.color, p.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p.offset;
            go.transform.localRotation = Quaternion.Euler(p.rotation);
            go.transform.localScale = p.size;
            var rend = go.GetComponent<Renderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // small parts: no shadows (faster)
            if (p.glow) KittyLook.MakeGlow(go, p.color);
            if (p.spin != 0f) go.AddComponent<Spinner>().degreesPerSecond = p.spin;
            if (layer >= 0) go.layer = layer;
            made.Add(go);
        }
        return made;
    }

    public static void SetLayer(GameObject root, int layer)
    {
        if (layer < 0) return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }
}

// Turns something around its own up axis forever (propellers, display stands).
public class Spinner : MonoBehaviour
{
    public float degreesPerSecond = 90f;
    void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
}
