using System.Collections;
using UnityEngine;

// KITTY BLASTER - a MOD-ONLY gun that shoots little black kittens.
//   Grip = hold it, Trigger = fire (1 kitten every 0.4 s, hold to keep firing).
//   Only works for a mod (ModPowers.Instance.IsMod). Anyone else gets a sad "mew" and it hops out of their hand after 1 s.
//   It is NOT on the weapon racks and has no WeaponData, so the normal weapon list never sees it.
//   Get one from the Mod Panel's KITTY button (or SPAWN / NEXT after running menu 7).
[RequireComponent(typeof(Grabbable))]
public class KittyBlaster : MonoBehaviour
{
    [Tooltip("Seconds between kittens.")]
    public float fireInterval = 0.4f;
    [Tooltip("How fast kittens leave the barrel.")]
    public float launchSpeed = 5f;
    [Tooltip("Extra upward speed so kittens fly in an arc.")]
    public float launchUpward = 2f;
    [Tooltip("Where kittens come out (the cat-mouth barrel), relative to the grip.")]
    public Vector3 barrelTip = new Vector3(0f, 0.035f, 0.225f);
    [Tooltip("Damage each kitten does per scratch (they scratch every 0.5 s for up to 3 s).")]
    public float scratchDamage = Kitten.DefaultScratchDamage;

    [Header("Optional sounds (leave empty to use the built-in ones)")]
    public AudioClip mrrpClip;
    public AudioClip mewClip;
    public AudioClip hissClip;

    Grabbable grab;
    float nextShot;
    float nextMew;
    Coroutine dropRoutine;

    void Awake()
    {
        grab = GetComponent<Grabbable>();
        grab.Grabbed += OnGrabbed;
        grab.Released += OnReleased;
        grab.TriggerPressed += hand => TryFire(hand, true);
        grab.TriggerHeld += hand => TryFire(hand, false);
    }

    // True if this player is the local mod. (Single-player for now: the mod is whoever has ModPowers with IsMod on.)
    public static bool IsModPlayer(GameObject player)
    {
        var mod = ModPowers.Instance;
        if (mod == null || !mod.IsMod || player == null) return false;
        return player == mod.gameObject || player.transform.IsChildOf(mod.transform) || mod.transform.IsChildOf(player.transform);
    }

    // ------------------------------------------------------------ grab / release

    void OnGrabbed(HandGrabber hand)
    {
        if (IsModPlayer(hand.PlayerRoot)) return;
        SadMew(hand);
    }

    void OnReleased(HandGrabber hand, Vector3 velocity)
    {
        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }
    }

    // Not a mod: sad mew, then it drops out of the hand after 1 second.
    void SadMew(HandGrabber hand)
    {
        if (Time.time >= nextMew)
        {
            nextMew = Time.time + 1f;
            KittySounds.Play(mewClip, KittySounds.Mew, transform.position, 0.6f);
        }
        if (dropRoutine == null) dropRoutine = StartCoroutine(DropSoon(hand));
    }

    IEnumerator DropSoon(HandGrabber hand)
    {
        yield return new WaitForSeconds(1f);
        dropRoutine = null;
        if (grab.Holder == hand && !IsModPlayer(hand.PlayerRoot))
            grab.ForceRelease();
    }

    // ------------------------------------------------------------ firing

    void TryFire(HandGrabber hand, bool justPressed)
    {
        if (!IsModPlayer(hand.PlayerRoot))
        {
            if (justPressed) SadMew(hand);   // does nothing for non-mods
            return;
        }
        if (Time.time < nextShot) return;
        nextShot = Time.time + fireInterval;

        // MULTIPLAYER NOTE: when multiplayer is added, this must NOT spawn kittens directly.
        // Instead, send a "fire kitty" request to the SERVER, and the server must check that the shooter
        // really is a mod (server-side list of mods - never trust the player's own game) before it spawns
        // the kittens for everyone. Otherwise a hacker could flip IsMod on their own copy and spam kittens.
        Vector3 tip = transform.TransformPoint(barrelTip);
        Vector3 fwd = transform.forward;
        Kitten.Spawn(tip + fwd * 0.05f, fwd * launchSpeed + Vector3.up * launchUpward, hand.PlayerRoot, hissClip, scratchDamage);

        hand.Buzz(0.25f, 0.06f);   // small haptic buzz
        KittySounds.Play(mrrpClip, KittySounds.Mrrp, tip, 0.6f);
        FxSpawner.Burst(tip, new Color(1f, 0.6f, 0.85f), 4, 1.2f, 0.03f);
    }

    // Flat-screen testing (L key on the keyboard, mods only): launches one kitten from the camera.
    public static void TestFireFromView(GameObject player)
    {
        var cam = Camera.main;
        if (cam == null || !IsModPlayer(player)) return;
        Vector3 fwd = cam.transform.forward;
        Vector3 start = cam.transform.position + fwd * 0.5f - Vector3.up * 0.3f;
        Kitten.Spawn(start, fwd * 5f + Vector3.up * 2f, player);
        KittySounds.Play(null, KittySounds.Mrrp, start, 0.6f);
    }

    // Testing only (J key, mods): drops a "wild" kitten that nobody owns 2.5 m in front of you.
    // Unlike your own kittens it CAN scratch you - handy for testing your health and the safe zone.
    public static void TestWildKitten(GameObject player)
    {
        if (!IsModPlayer(player) || Camera.main == null) return;
        Vector3 fwd = Camera.main.transform.forward;
        fwd.y = 0f;
        fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : player.transform.forward;
        Vector3 pos = player.transform.position + fwd * 2.5f + Vector3.up * 0.6f;
        Kitten.Spawn(pos, Vector3.up, null);
        KittySounds.Play(null, KittySounds.Mrrp, pos, 0.6f);
    }

    // ------------------------------------------------------------ building one (from shapes)

    // Builds a complete Kitty Blaster. Used by the editor menu (to save the prefab) and by the
    // Mod Panel if no prefab has been set yet.
    public static GameObject Create(Vector3 position, Quaternion rotation)
    {
        var root = new GameObject("Kitty Blaster");
        root.transform.SetPositionAndRotation(position, rotation);

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.025f, 0.085f);
        box.size = new Vector3(0.09f, 0.17f, 0.32f);

        root.AddComponent<Grabbable>();
        root.AddComponent<KittyBlaster>();
        BuildLook(root.transform);
        return root;
    }

    // Forward (+Z) is where kittens come out. The handle sits at the grip point (0,0,0).
    static void BuildLook(Transform root)
    {
        var v = new GameObject("Visual").transform;
        v.SetParent(root, false);

        Color purple = new Color(0.55f, 0.3f, 0.85f);
        Color darkPurple = new Color(0.36f, 0.17f, 0.6f);
        Color lightPurple = new Color(0.74f, 0.52f, 0.97f);
        Color pink = new Color(1f, 0.56f, 0.76f);
        Color mouth = new Color(0.16f, 0.05f, 0.13f);
        Color white = new Color(0.97f, 0.97f, 1f);
        var C = PrimitiveType.Cube;
        var S = PrimitiveType.Sphere;
        var Y = PrimitiveType.Cylinder;

        // chunky body + handle + trigger
        KittyLook.Part(v, C, "Handle", new Vector3(0f, -0.045f, -0.005f), new Vector3(0.04f, 0.11f, 0.05f), new Vector3(15f, 0f, 0f), darkPurple);
        KittyLook.Part(v, C, "Body", new Vector3(0f, 0.04f, 0.07f), new Vector3(0.085f, 0.09f, 0.2f), Vector3.zero, purple);
        KittyLook.Part(v, S, "Back Cap", new Vector3(0f, 0.04f, -0.03f), new Vector3(0.085f, 0.09f, 0.06f), Vector3.zero, purple);
        KittyLook.Part(v, C, "Trigger", new Vector3(0f, -0.012f, 0.035f), new Vector3(0.012f, 0.025f, 0.012f), Vector3.zero, darkPurple);

        // round barrel shaped like a cat's mouth (with nose, tongue and whiskers)
        KittyLook.Part(v, Y, "Barrel", new Vector3(0f, 0.04f, 0.19f), new Vector3(0.095f, 0.025f, 0.095f), new Vector3(90f, 0f, 0f), lightPurple);
        KittyLook.Part(v, Y, "Mouth", new Vector3(0f, 0.035f, 0.2165f), new Vector3(0.06f, 0.003f, 0.06f), new Vector3(90f, 0f, 0f), mouth);
        KittyLook.Part(v, S, "Tongue", new Vector3(0f, 0.02f, 0.218f), new Vector3(0.025f, 0.012f, 0.008f), Vector3.zero, pink);
        KittyLook.Part(v, S, "Nose", new Vector3(0f, 0.072f, 0.215f), new Vector3(0.016f, 0.011f, 0.01f), Vector3.zero, pink);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
                KittyLook.Part(v, C, "Whisker", new Vector3(side * 0.062f, 0.052f - i * 0.009f, 0.212f),
                               new Vector3(0.04f, 0.0025f, 0.0025f), new Vector3(0f, 0f, side * (i - 1) * -12f), white);

        // two triangle cat ears (a cube turned 45 degrees, half hidden in the body = a triangle)
        for (int side = -1; side <= 1; side += 2)
        {
            KittyLook.Part(v, C, "Ear", new Vector3(side * 0.025f, 0.085f, 0.03f), new Vector3(0.045f, 0.045f, 0.02f), new Vector3(0f, 0f, 45f), purple);
            KittyLook.Part(v, C, "Inner Ear", new Vector3(side * 0.025f, 0.087f, 0.0405f), new Vector3(0.027f, 0.027f, 0.004f), new Vector3(0f, 0f, 45f), pink);
        }

        // pink paw print on both sides
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 0.0435f;
            KittyLook.Part(v, S, "Paw Pad", new Vector3(x, 0.03f, 0.07f), new Vector3(0.004f, 0.026f, 0.03f), Vector3.zero, pink);
            KittyLook.Part(v, S, "Toe Bean", new Vector3(x, 0.053f, 0.049f), new Vector3(0.004f, 0.012f, 0.012f), Vector3.zero, pink);
            KittyLook.Part(v, S, "Toe Bean", new Vector3(x, 0.06f, 0.063f), new Vector3(0.004f, 0.012f, 0.012f), Vector3.zero, pink);
            KittyLook.Part(v, S, "Toe Bean", new Vector3(x, 0.06f, 0.077f), new Vector3(0.004f, 0.012f, 0.012f), Vector3.zero, pink);
            KittyLook.Part(v, S, "Toe Bean", new Vector3(x, 0.053f, 0.091f), new Vector3(0.004f, 0.012f, 0.012f), Vector3.zero, pink);
        }
    }
}
