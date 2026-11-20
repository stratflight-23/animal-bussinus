using TMPro;
using UnityEngine;

// The Pet House panel: poke the buttons with your hand.
//   PREV / NEXT  browse pets (the selected one shows on the little stand)
//   SUMMON       it comes out and follows you      PUT AWAY   send it home
// Every pet is FREE. No headset? Near the Pet House: N / M = prev / next, P = summon, O = put away.
public class PetHouse : MonoBehaviour
{
    public PetLibrary library;
    [Tooltip("Where the preview pet stands.")]
    public Transform previewSpot;

    TextMeshPro display;
    int index;
    GameObject preview;
    Transform previewTail;
    float messageUntil;

    void Start()
    {
        if (library == null) library = PetLibrary.Load();
        PokePanel.EnsureHandPokers();
        BuildPanel();
        RefreshPreview();
        Refresh();
    }

    PetData Selected
    {
        get
        {
            if (library == null || library.all.Count == 0) return null;
            index = (index % library.all.Count + library.all.Count) % library.all.Count;
            return library.all[index];
        }
    }

    void Next() { index++; RefreshPreview(); Refresh(); }
    void Prev() { index--; RefreshPreview(); Refresh(); }

    void Summon()
    {
        var pets = PlayerPets.Local;
        var p = Selected;
        if (pets == null || p == null) { Flash("NO PLAYER FOUND"); return; }
        pets.Summon(p);   // free!
        SoundFx.Play(Sfx.Chime, transform.position, 0.4f);
        Flash(p.displayName.ToUpper() + " IS FOLLOWING YOU!");
    }

    void PutAway()
    {
        var pets = PlayerPets.Local;
        if (pets == null) return;
        pets.PutAway();
        Flash("PET WENT HOME");
    }

    void Update()
    {
        if (messageUntil > 0f && Time.time >= messageUntil) { messageUntil = 0f; Refresh(); }
        if (previewTail != null) previewTail.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 6f) * 20f, 0f);

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !PokePanel.PlayerNear(transform.position, 8f)) return;
        if (kb.nKey.wasPressedThisFrame) Prev();
        if (kb.mKey.wasPressedThisFrame) Next();
        if (kb.pKey.wasPressedThisFrame) Summon();
        if (kb.oKey.wasPressedThisFrame) PutAway();
    }

    void Flash(string msg)
    {
        if (display == null) return;
        display.text = "<size=80%>" + msg + "</size>";
        messageUntil = Time.time + 1.5f;
    }

    void Refresh()
    {
        if (display == null || messageUntil > 0f) return;
        var p = Selected;
        if (p == null) { display.text = "PET HOUSE\n<size=70%>(run Gorilla Company > 12. Generate Pets)</size>"; return; }
        bool out_ = PlayerPets.Local != null && PlayerPets.Local.Current == p;
        display.text = "<size=70%>PET " + (index + 1) + "/" + library.all.Count + "</size>\n<b>" + p.displayName + "</b>\n<size=65%>" +
                       "<color=#7CFF8A>FREE</color> · trick: " + p.idleTrick + (p.flies ? " · flies" : "") +
                       (out_ ? "  ·  <color=#FFD94D>WITH YOU</color>" : "") + "</size>";
    }

    void RefreshPreview()
    {
        if (preview != null) Destroy(preview);
        var p = Selected;
        if (previewSpot == null || p == null) return;
        preview = new GameObject("Preview Pet");
        preview.transform.SetParent(previewSpot, false);
        preview.AddComponent<Spinner>().degreesPerSecond = 30f;
        PetFollower.BuildVisual(p, preview.transform, out previewTail, null);
    }

    void BuildPanel()
    {
        PokePanel.Backing(transform, new Vector2(0.46f, 0.42f), new Color(0.12f, 0.14f, 0.1f));
        display = PokePanel.Text(transform, "Display", new Vector3(0f, 0.11f, -0.01f), 0.32f, new Vector2(0.42f, 0.16f));
        var half = new Vector2(0.2f, 0.07f);
        PokePanel.Button(transform, "PREV", new Vector3(-0.11f, -0.03f, 0f), half, PokePanel.Grey, Prev);
        PokePanel.Button(transform, "NEXT", new Vector3(0.11f, -0.03f, 0f), half, PokePanel.Grey, Next);
        PokePanel.Button(transform, "SUMMON", new Vector3(-0.11f, -0.13f, 0f), half, PokePanel.Green, Summon);
        PokePanel.Button(transform, "PUT AWAY", new Vector3(0.11f, -0.13f, 0f), half, PokePanel.Red, PutAway);
    }
}
