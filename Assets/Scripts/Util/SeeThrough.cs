using UnityEngine;

// Makes see-through (transparent, glowing) shapes, like the safe zone ring and the red damage flash.
// Uses Unity's built-in "Sprites/Default" shader: it ignores lighting, supports transparency and is
// always included in builds (Quest too). Each shape's color + transparency is set with PrimitiveColor.
public static class SeeThrough
{
    static Material material;

    static Material Material
    {
        get
        {
            if (material == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) return null;
                material = new Material(shader) { name = "See Through" };
            }
            return material;
        }
    }

    // A colored see-through shape with no collider. Color alpha = how see-through (0 = invisible, 1 = solid).
    public static GameObject MakeShape(PrimitiveType type, Color color, string name, Transform parent)
    {
        var go = PrimitiveColor.MakeShape(type, color, name);
        if (parent != null) go.transform.SetParent(parent, false);
        var rend = go.GetComponent<Renderer>();
        var mat = Material;
        if (mat != null) rend.sharedMaterial = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        go.GetComponent<PrimitiveColor>().SetColor(color);
        return go;
    }
}
