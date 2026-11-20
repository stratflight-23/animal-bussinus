using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The Wardrobe panel: poke the buttons with your hand.
//   < SLOT >   pick Head / Face / Neck / Back / Hands / Body Color
//   PREV/NEXT  browse items in that slot (a preview spins on the little stand)
//   WEAR       put it on      REMOVE   take off what's in this slot
// Everything is FREE. Look in the mirror to see yourself!
// No headset? When you're near the wardrobe: C = next slot, Z / X = prev / next item, V = wear, B = remove.
public class Wardrobe : MonoBehaviour
{
    public CosmeticLibrary library;
    [Tooltip("Where the spinning preview of the selected item appears.")]
    public Transform previewSpot;

    static readonly CosmeticSlot[] Slots =
        { CosmeticSlot.Head, CosmeticSlot.Face, CosmeticSlot.Neck, CosmeticSlot.Back, CosmeticSlot.Hands, CosmeticSlot.BodyColor };

    TextMeshPro display;
    int slotIndex;
    readonly int[] itemIndex = new int[6];
    GameObject preview;
    float messageUntil;

    void Start()
    {
        if (library == null) library = CosmeticLibrary.Load();
        PokePanel.EnsureHandPokers();
        BuildPanel();
        RefreshPreview();
        Refresh();
    }

    CosmeticSlot Slot => Slots[slotIndex];

    List<CosmeticData> Items => library != null ? library.ForSlot(Slot) : new List<CosmeticData>();

    CosmeticData Selected
    {
        get
        {
            var items = Items;
            if (items.Count == 0) return null;
            itemIndex[slotIndex] = (itemIndex[slotIndex] % items.Count + items.Count) % items.Count;
            return items[itemIndex[slotIndex]];
        }
    }

    // ------------------------------------------------------------ buttons

    void NextSlot() { slotIndex = (slotIndex + 1) % Slots.Length; Changed(); }
    void PrevSlot() { slotIndex = (slotIndex + Slots.Length - 1) % Slots.Length; Changed(); }
    void NextItem() { itemIndex[slotIndex]++; Changed(); }
    void PrevItem() { itemIndex[slotIndex]--; Changed(); }

    void Wear()
    {
        var avatar = PlayerAvatar.Local;
        var c = Selected;
        if (avatar == null || c == null) { Flash("NO PLAYER FOUND"); return; }
        avatar.Wear(c);   // free! (c.price is 0 for everything right now)
        SoundFx.Play(Sfx.Chime, transform.position, 0.4f);
        FxSpawner.Confetti(transform.position + Vector3.up * 0.4f, 12);
        Flash("WEARING " + c.displayName.ToUpper());
    }

    void Remove()
    {
        var avatar = PlayerAvatar.Local;
        if (avatar == null) return;
        avatar.Remove(Slot);
        Flash("REMOVED");
    }

    void Changed()
    {
        RefreshPreview();
        Refresh();
    }

    // ------------------------------------------------------------ display + preview

    void Update()
    {
        if (messageUntil > 0f && Time.time >= messageUntil) { messageUntil = 0f; Refresh(); }

        // flat-screen test keys (only when you're near the wardrobe)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !PokePanel.PlayerNear(transform.position, 8f)) return;
        if (kb.cKey.wasPressedThisFrame) NextSlot();
        if (kb.zKey.wasPressedThisFrame) PrevItem();
        if (kb.xKey.wasPressedThisFrame) NextItem();
        if (kb.vKey.wasPressedThisFrame) Wear();
        if (kb.bKey.wasPressedThisFrame) Remove();
    }

    void Flash(string msg)
    {
        if (display == null) return;
        display.text = msg;
        messageUntil = Time.time + 1.3f;
    }

    void Refresh()
    {
        if (display == null || messageUntil > 0f) return;
        var items = Items;
        var c = Selected;
        string slotName = Slot == CosmeticSlot.BodyColor ? "BODY COLOR" : Slot.ToString().ToUpper();
        if (c == null)
        {
            display.text = slotName + "\n<size=70%>(run Gorilla Company > 10. Generate Cosmetics)</size>";
            return;
        }
        bool wearing = PlayerAvatar.Local != null && PlayerAvatar.Local.Worn(Slot) == c;
        display.text = "<size=70%>" + slotName + "  " + (itemIndex[slotIndex] + 1) + "/" + items.Count + "</size>\n<b>" +
                       c.displayName + "</b>\n<size=70%>" + (c.IsFree ? "<color=#7CFF8A>FREE</color>" : c.price + " coins") +
                       (wearing ? "  ·  <color=#FFD94D>WEARING</color>" : "") + "</size>";
    }

    void RefreshPreview()
    {
        if (preview != null) Destroy(preview);
        if (previewSpot == null) return;
        var c = Selected;
        if (c == null) return;

        preview = new GameObject("Preview");
        preview.transform.SetParent(previewSpot, false);
        preview.AddComponent<Spinner>().degreesPerSecond = 40f;
        var holder = new GameObject("Item").transform;
        holder.SetParent(preview.transform, false);
        holder.localScale = Vector3.one * 1.6f;   // a bit bigger so it's easy to see

        if (c.slot == CosmeticSlot.BodyColor)
        {
            var ball = PrimitiveColor.MakeShape(PrimitiveType.Sphere, c.bodyColor, "Color Ball");
            ball.transform.SetParent(holder, false);
            ball.transform.localScale = Vector3.one * 0.15f;
            if (c.bodyStyle == BodyColorStyle.Glow) KittyLook.MakeGlow(ball, c.bodyColor);
            if (c.bodyStyle == BodyColorStyle.Rainbow) ball.AddComponent<RainbowTint>();
        }
        else
        {
            // a little stand-in head so hats and glasses sit on something
            if (c.slot == CosmeticSlot.Head || c.slot == CosmeticSlot.Face)
            {
                var head = PrimitiveColor.MakeShape(PrimitiveType.Sphere, new Color(0.27f, 0.24f, 0.22f), "Dummy Head");
                head.transform.SetParent(holder, false);
                head.transform.localScale = Vector3.one * 0.24f;
            }
            PartRecipe.Build(c.parts, holder);
        }
    }

    // ------------------------------------------------------------ building the panel

    void BuildPanel()
    {
        PokePanel.Backing(transform, new Vector2(0.46f, 0.5f), new Color(0.12f, 0.1f, 0.15f));
        display = PokePanel.Text(transform, "Display", new Vector3(0f, 0.15f, -0.01f), 0.32f, new Vector2(0.42f, 0.16f));
        var half = new Vector2(0.2f, 0.07f);
        PokePanel.Button(transform, "< SLOT", new Vector3(-0.11f, 0.02f, 0f), half, PokePanel.Blue, PrevSlot);
        PokePanel.Button(transform, "SLOT >", new Vector3(0.11f, 0.02f, 0f), half, PokePanel.Blue, NextSlot);
        PokePanel.Button(transform, "PREV", new Vector3(-0.11f, -0.07f, 0f), half, PokePanel.Grey, PrevItem);
        PokePanel.Button(transform, "NEXT", new Vector3(0.11f, -0.07f, 0f), half, PokePanel.Grey, NextItem);
        PokePanel.Button(transform, "WEAR", new Vector3(-0.11f, -0.17f, 0f), half, PokePanel.Green, Wear);
        PokePanel.Button(transform, "REMOVE", new Vector3(0.11f, -0.17f, 0f), half, PokePanel.Red, Remove);
    }
}

// Slowly cycles through rainbow colors (for the Rainbow body color preview).
public class RainbowTint : MonoBehaviour
{
    PrimitiveColor pc;
    void Start() => pc = GetComponent<PrimitiveColor>();
    void Update() { if (pc != null) pc.SetColor(Color.HSVToRGB(Mathf.Repeat(Time.time * 0.15f, 1f), 0.75f, 1f)); }
}
