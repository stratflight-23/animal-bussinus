using UnityEngine;

// Colors one shape (cube, sphere...) without needing a separate material file for every color.
// Also lets effects tint it for a moment (blue when frozen, orange when burning...).
[ExecuteAlways, DisallowMultipleComponent]
public class PrimitiveColor : MonoBehaviour
{
    public Color color = Color.white;

    static MaterialPropertyBlock block;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP shaders
    static readonly int ColorId = Shader.PropertyToID("_Color");         // older shaders

    Renderer rend;
    bool tinted;
    Color tint;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    public void SetColor(Color c)
    {
        color = c;
        Apply();
    }

    public void SetTint(Color c)
    {
        tinted = true;
        tint = c;
        Apply();
    }

    public void ClearTint()
    {
        if (!tinted) return;
        tinted = false;
        Apply();
    }

    public void Apply()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend == null) return;
        if (block == null) block = new MaterialPropertyBlock();

        Color c = tinted ? Color.Lerp(color, tint, 0.65f) : color;
        rend.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(ColorId, c);
        rend.SetPropertyBlock(block);
    }

    // Makes a colored primitive shape with no collider. Used for effects and quick visuals.
    public static GameObject MakeShape(PrimitiveType type, Color c, string name = null)
    {
        var go = GameObject.CreatePrimitive(type);
        if (name != null) go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) { col.enabled = false; Kill(col); }
        go.AddComponent<PrimitiveColor>().SetColor(c);
        return go;
    }

    // Destroy that works both while playing and in the editor.
    public static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }
}
