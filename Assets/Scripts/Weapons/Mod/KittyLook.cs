using System.Collections.Generic;
using UnityEngine;

// Little helpers for building the Kitty Blaster and the kittens out of simple shapes.
public static class KittyLook
{
    static readonly Dictionary<Color, Material> glowMaterials = new Dictionary<Color, Material>();

    // Makes one colored shape (no collider) as a child of "parent".
    public static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale,
                                  Vector3 euler, Color color)
    {
        var go = PrimitiveColor.MakeShape(type, color, name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        return go;
    }

    // Makes a shape "glow": it ignores lighting so it's always bright (kitten eyes, scratch marks).
    // Only used while playing. If the unlit shader isn't in the build, it just stays a normal bright color.
    public static void MakeGlow(GameObject go, Color color)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        if (!Application.isPlaying) return;

        if (!glowMaterials.TryGetValue(color, out var mat) || mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            mat = new Material(shader) { color = color, name = "Kitty Glow" };
            glowMaterials[color] = mat;
        }
        rend.sharedMaterial = mat;
    }
}
