using System.Collections.Generic;
using UnityEngine;

// Your gorilla's body + the cosmetics you're wearing. Added to GorillaPlayer automatically when the game starts.
//   - Uses our low-poly gorilla (GorillaModel, from Assets/Characters/LowPolyGorilla.prefab) if it's on the player.
//     If a scene doesn't have it yet, it builds a simple stand-in body from shapes instead.
//   - Your own head, body, hats, face, neck and back items are on the "SelfHidden" layer, which YOUR camera skips,
//     so nothing blocks your view. Mirrors (and later, other players) still see them. Gloves stay visible to you.
//   - Body Color tints your head, body and hand spheres.
//   - What you wear is saved (PlayerPrefs) and put back on next time.
//
// MULTIPLAYER NOTE: when multiplayer is added, the cosmetic ids you're wearing (and your pet, see PlayerPets)
// must be synced to the other players (e.g. sent when you join and whenever you change), so they can see them.
public class PlayerAvatar : MonoBehaviour
{
    public static PlayerAvatar Local { get; private set; }

    public const string SelfHiddenLayerName = "SelfHidden";
    const string SaveKeyPrefix = "Cosmetic_";

    [Tooltip("Normal fur color (when no Body Color cosmetic is worn).")]
    public Color furColor = new Color(0.27f, 0.24f, 0.22f);
    public Color faceColor = new Color(0.55f, 0.47f, 0.4f);

    CosmeticLibrary library;
    Transform cam;
    Transform headAnchor, neckAnchor, bodyAnchor;
    readonly List<Transform> handHolders = new List<Transform>();
    int hiddenLayer = -1;

    // fur renderers + their normal materials (body color changes these)
    readonly List<Renderer> fur = new List<Renderer>();
    readonly List<Material> furMaterials = new List<Material>();
    readonly List<PrimitiveColor> furColors = new List<PrimitiveColor>();
    readonly List<float> furShade = new List<float>();
    GorillaModel model;

    readonly Dictionary<CosmeticSlot, CosmeticData> worn = new Dictionary<CosmeticSlot, CosmeticData>();
    readonly Dictionary<CosmeticSlot, List<GameObject>> wornObjects = new Dictionary<CosmeticSlot, List<GameObject>>();

    void Awake() => Local = this;

    void OnDestroy()
    {
        Application.onBeforeRender -= UpdateAnchors;
        if (Local == this) Local = null;
    }

    void Start()
    {
        library = CosmeticLibrary.Load();
        var camera = GetComponentInChildren<Camera>();
        if (camera == null) camera = Camera.main;
        cam = camera != null ? camera.transform : transform;

        hiddenLayer = LayerMask.NameToLayer(SelfHiddenLayerName);
        if (camera != null && hiddenLayer >= 0) camera.cullingMask &= ~(1 << hiddenLayer); // don't draw your own head

        model = GetComponentInChildren<GorillaModel>(true);
        if (model != null)
        {
            furColor = model.bodyColor;
            foreach (var p in model.tintParts)
                if (p != null && p.renderer != null) AddFur(p.renderer.gameObject, p.shade);
            SetFurTint(furColor);
        }
        else
        {
            BuildBody();
            UpdateAnchors();
            Application.onBeforeRender += UpdateAnchors;
        }
        LoadSaved();
    }

    void LateUpdate()
    {
        if (model == null) UpdateAnchors();
        if (worn.TryGetValue(CosmeticSlot.BodyColor, out var bc) && bc.bodyStyle == BodyColorStyle.Rainbow)
            SetFurTint(Color.HSVToRGB(Mathf.Repeat(Time.time * 0.15f, 1f), 0.75f, 1f));
    }

    // ------------------------------------------------------------ wearing things

    public CosmeticData Worn(CosmeticSlot slot) => worn.TryGetValue(slot, out var c) ? c : null;

    public void Wear(CosmeticData c, bool save = true)
    {
        if (c == null) return;
        Remove(c.slot, false);
        worn[c.slot] = c;

        if (c.slot == CosmeticSlot.BodyColor)
        {
            ApplyBodyColor(c);
        }
        else
        {
            var made = new List<GameObject>();
            if (c.slot == CosmeticSlot.Hands)
            {
                foreach (var holder in HandPoints())
                    made.Add(BuildItem(c, holder, -1, Vector3.zero));          // gloves: visible to you too
            }
            else
            {
                Transform anchor = AttachPoint(c.slot, out Vector3 offset);
                made.Add(BuildItem(c, anchor, hiddenLayer, offset));         // hidden from your own camera
            }
            wornObjects[c.slot] = made;
        }

        if (save)
        {
            PlayerPrefs.SetString(SaveKeyPrefix + c.slot, c.id);
            PlayerPrefs.Save();
        }
    }

    public void Remove(CosmeticSlot slot, bool save = true)
    {
        if (wornObjects.TryGetValue(slot, out var objs))
        {
            foreach (var o in objs) if (o != null) Destroy(o);
            wornObjects.Remove(slot);
        }
        bool hadBodyColor = slot == CosmeticSlot.BodyColor && worn.ContainsKey(slot);
        worn.Remove(slot);
        if (hadBodyColor) ApplyBodyColor(null);

        if (save)
        {
            PlayerPrefs.DeleteKey(SaveKeyPrefix + slot);
            PlayerPrefs.Save();
        }
    }

    void LoadSaved()
    {
        if (library == null) return;
        foreach (CosmeticSlot slot in System.Enum.GetValues(typeof(CosmeticSlot)))
        {
            string id = PlayerPrefs.GetString(SaveKeyPrefix + slot, "");
            if (id.Length == 0) continue;
            var c = library.Find(id);
            if (c != null) Wear(c, false);
        }
    }

    // Where each slot's items go. Cosmetic recipes are measured from the head's middle / under the chin / the body's
    // middle, so the model's attachment points (which sit ON the surface and are scaled up for the bigger gorilla)
    // get a small offset back to those spots.
    Transform AttachPoint(CosmeticSlot slot, out Vector3 offset)
    {
        offset = Vector3.zero;
        if (model == null)
            return slot == CosmeticSlot.Neck ? neckAnchor : slot == CosmeticSlot.Back ? bodyAnchor : headAnchor;
        switch (slot)
        {
            case CosmeticSlot.Head: offset = new Vector3(0f, -0.12f, 0f); return model.headTop;
            case CosmeticSlot.Face: offset = new Vector3(0f, 0f, -0.12f); return model.face;
            case CosmeticSlot.Neck: return model.neck;
            case CosmeticSlot.Back: offset = new Vector3(0f, 0f, 0.14f); return model.back;
            default: return model.chest;
        }
    }

    IEnumerable<Transform> HandPoints()
    {
        if (model == null) return handHolders;
        return new[] { model.leftHandPoint, model.rightHandPoint };
    }

    GameObject BuildItem(CosmeticData c, Transform anchor, int layer, Vector3 offset)
    {
        var root = new GameObject(c.displayName);
        root.transform.SetParent(anchor, false);
        root.transform.localPosition = offset;
        PartRecipe.Build(c.parts, root.transform, layer);
        if (layer >= 0) root.layer = layer;
        return root;
    }

    // ------------------------------------------------------------ body color

    void ApplyBodyColor(CosmeticData c)
    {
        for (int i = 0; i < fur.Count; i++)
            if (fur[i] != null) fur[i].sharedMaterial = furMaterials[i];   // back to normal first

        if (c == null) { SetFurTint(furColor); return; }
        switch (c.bodyStyle)
        {
            case BodyColorStyle.Glow:
                foreach (var r in fur) if (r != null) KittyLook.MakeGlow(r.gameObject, c.bodyColor);
                SetFurTint(c.bodyColor);
                break;
            case BodyColorStyle.Rainbow:
                break; // animated in LateUpdate
            default:
                SetFurTint(c.bodyColor);
                break;
        }
    }

    void SetFurTint(Color color)
    {
        for (int i = 0; i < furColors.Count; i++)
        {
            if (furColors[i] == null) continue;
            Color c = color * furShade[i];   // darker facets stay darker
            c.a = 1f;
            furColors[i].SetColor(c);
        }
    }

    // ------------------------------------------------------------ the body

    void BuildBody()
    {
        headAnchor = new GameObject("Avatar Head").transform;
        neckAnchor = new GameObject("Avatar Neck").transform;
        bodyAnchor = new GameObject("Avatar Body").transform;
        foreach (var a in new[] { headAnchor, neckAnchor, bodyAnchor }) a.SetParent(transform, false);

        // furry round head with a lighter face, eyes and a brow
        AddFur(Part(headAnchor, PrimitiveType.Sphere, "Head", Vector3.zero, Vector3.one * 0.24f, furColor));
        Part(headAnchor, PrimitiveType.Sphere, "Face", new Vector3(0f, -0.02f, 0.075f), new Vector3(0.17f, 0.14f, 0.1f), faceColor);
        Part(headAnchor, PrimitiveType.Sphere, "Eye", new Vector3(-0.04f, 0.015f, 0.115f), Vector3.one * 0.03f, Color.black);
        Part(headAnchor, PrimitiveType.Sphere, "Eye", new Vector3(0.04f, 0.015f, 0.115f), Vector3.one * 0.03f, Color.black);
        AddFur(Part(headAnchor, PrimitiveType.Cube, "Brow", new Vector3(0f, 0.045f, 0.1f), new Vector3(0.15f, 0.03f, 0.05f), furColor));
        Part(headAnchor, PrimitiveType.Sphere, "Nose", new Vector3(0f, -0.025f, 0.125f), new Vector3(0.05f, 0.025f, 0.02f), Color.black * 0.8f);

        // body + shoulders
        AddFur(Part(bodyAnchor, PrimitiveType.Capsule, "Body", Vector3.zero, new Vector3(0.36f, 0.25f, 0.28f), furColor));
        AddFur(Part(bodyAnchor, PrimitiveType.Sphere, "Shoulders", new Vector3(0f, 0.17f, 0f), new Vector3(0.46f, 0.16f, 0.26f), furColor));
        Part(bodyAnchor, PrimitiveType.Sphere, "Chest", new Vector3(0f, 0.02f, 0.11f), new Vector3(0.24f, 0.26f, 0.1f), faceColor);

        foreach (var a in new[] { headAnchor, neckAnchor, bodyAnchor }) PartRecipe.SetLayer(a.gameObject, hiddenLayer);

        // hands: tint the existing hand spheres, and make an unscaled spot for gloves
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Left Hand" && t.name != "Right Hand") continue;
            var r = t.GetComponent<Renderer>();
            if (r != null)
            {
                var pc = t.GetComponent<PrimitiveColor>();
                if (pc == null) pc = t.gameObject.AddComponent<PrimitiveColor>();
                fur.Add(r);
                furMaterials.Add(r.sharedMaterial);
                furColors.Add(pc);
                furShade.Add(1f);
            }
            var holder = new GameObject("Glove Spot").transform;
            holder.SetParent(t, false);
            holder.localScale = Vector3.one / Mathf.Max(0.0001f, t.lossyScale.x);
            handHolders.Add(holder);
        }
        SetFurTint(furColor);
    }

    GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = PrimitiveColor.MakeShape(type, color, name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    void AddFur(GameObject go, float shade = 1f)
    {
        var r = go.GetComponent<Renderer>();
        var pc = go.GetComponent<PrimitiveColor>();
        if (pc == null) pc = go.AddComponent<PrimitiveColor>();
        fur.Add(r);
        furMaterials.Add(r.sharedMaterial);
        furColors.Add(pc);
        furShade.Add(shade);
    }

    // Head follows the headset; neck and body hang below it and only turn left/right.
    void UpdateAnchors()
    {
        if (cam == null || headAnchor == null) return;
        Vector3 fwd = cam.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = cam.up;
        fwd.y = 0f;
        Quaternion yaw = Quaternion.LookRotation(fwd.sqrMagnitude > 0.001f ? fwd.normalized : transform.forward);

        headAnchor.SetPositionAndRotation(cam.position - cam.forward * 0.07f, cam.rotation);
        neckAnchor.SetPositionAndRotation(cam.position + Vector3.down * 0.2f - yaw * Vector3.forward * 0.05f, yaw);
        bodyAnchor.SetPositionAndRotation(cam.position + Vector3.down * 0.5f - yaw * Vector3.forward * 0.08f, yaw);
    }
}
