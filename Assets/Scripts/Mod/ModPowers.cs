using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Put this on the player (GorillaPlayer). Holds all the mod powers.
// Powers only work after the code is entered on the Mod Panel (ModKeypad).
public class ModPowers : MonoBehaviour
{
    public static ModPowers Instance { get; private set; }

    [Header("Fly")]
    public float flySpeed = 6f;

    [Header("Spawn")]
    [Tooltip("Drag prefabs here to make them spawnable. If empty, spawns basic shapes.")]
    public GameObject[] spawnables;
    public float spawnDistance = 0.6f;

    [Header("Coins")]
    public int coinButtonAmount = 100;

    [Header("Kitty Blaster (mod only)")]
    [Tooltip("Set by Gorilla Company > 7. If empty, the KITTY button builds one from shapes instead.")]
    public GameObject kittyBlasterPrefab;

    public bool IsMod { get; private set; }
    public bool IsFlying { get; private set; }

    Rigidbody rb;
    gorillalocomotion loco;
    Transform head;
    Transform rightHand;
    int spawnIndex;

    static readonly PrimitiveType[] defaultShapes =
        { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule, PrimitiveType.Cylinder };

    const string BanKey = "BannedPlayers";

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
        loco = GetComponent<gorillalocomotion>();
    }

    void Start()
    {
        if (Camera.main != null) head = Camera.main.transform;
        var rh = GameObject.Find("Right Hand");
        if (rh != null) rightHand = rh.transform;

        // Make sure the Kitty Blaster is in the SPAWN / NEXT list too (only if the list is already in use,
        // so the default test shapes don't disappear).
        if (kittyBlasterPrefab != null && HasPrefabs && System.Array.IndexOf(spawnables, kittyBlasterPrefab) < 0)
        {
            var list = new List<GameObject>(spawnables) { kittyBlasterPrefab };
            spawnables = list.ToArray();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------ mod on/off

    public void Unlock()
    {
        IsMod = true;
        Debug.Log("[Mod] Mod powers unlocked");
    }

    public void Lock()
    {
        if (IsFlying) SetFlying(false);
        IsMod = false;
    }

    // ------------------------------------------------------------ fly

    public void ToggleFly()
    {
        if (!IsMod) return;
        SetFlying(!IsFlying);
    }

    void SetFlying(bool on)
    {
        IsFlying = on;
        if (loco != null) loco.enabled = !on;   // normal walking is off while flying
        rb.useGravity = !on;
        if (on) rb.linearVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (!IsFlying) return;

        // If something turned walking back on (like respawning after lava), stop flying
        if (loco != null && loco.enabled)
        {
            IsFlying = false;
            rb.useGravity = true;
            return;
        }

        // Left stick = fly where you look, right stick up/down = go up/down
        Vector2 move = Vector2.zero;
        float vertical = 0f;
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 l)) move = l;
        if (right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 r)) vertical = r.y;

        // Keyboard for testing without a headset: WASD + Space (up) / Left Ctrl (down)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed) move.y -= 1f;
            if (kb.dKey.isPressed) move.x += 1f;
            if (kb.aKey.isPressed) move.x -= 1f;
            if (kb.spaceKey.isPressed) vertical += 1f;
            if (kb.leftCtrlKey.isPressed) vertical -= 1f;
        }

        Transform view = head != null ? head : transform;
        Vector3 target = (view.forward * move.y + view.right * move.x + Vector3.up * vertical) * flySpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, target, 10f * Time.fixedDeltaTime);
    }

    // ------------------------------------------------------------ spawn

    bool HasPrefabs => spawnables != null && spawnables.Length > 0;
    int SpawnCount => HasPrefabs ? spawnables.Length : defaultShapes.Length;

    public string CurrentSpawnName
    {
        get
        {
            if (spawnIndex >= SpawnCount) spawnIndex = 0;
            if (HasPrefabs) return spawnables[spawnIndex] != null ? spawnables[spawnIndex].name : "(empty)";
            return defaultShapes[spawnIndex].ToString();
        }
    }

    public void NextSpawn()
    {
        spawnIndex = (spawnIndex + 1) % SpawnCount;
    }

    public GameObject Spawn()
    {
        if (!IsMod) return null;
        if (spawnIndex >= SpawnCount) spawnIndex = 0;

        Transform view = head != null ? head : transform;
        Vector3 fwd = view.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
        fwd.Normalize();
        Vector3 pos = (rightHand != null ? rightHand.position : view.position) + fwd * spawnDistance;

        GameObject obj;
        if (HasPrefabs)
        {
            if (spawnables[spawnIndex] == null) return null;
            obj = Instantiate(spawnables[spawnIndex], pos, Quaternion.LookRotation(fwd));
        }
        else
        {
            obj = GameObject.CreatePrimitive(defaultShapes[spawnIndex]);
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * 0.35f;
            obj.AddComponent<Rigidbody>();
            obj.GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
        }
        obj.name = "Spawned " + obj.name;
        return obj;
    }

    // ------------------------------------------------------------ kitty blaster

    // Spawns a Kitty Blaster floating just in front of the right hand. Mods only.
    // MULTIPLAYER NOTE: the server must check the player is a real mod before spawning this for everyone.
    public GameObject SpawnKittyBlaster()
    {
        if (!IsMod) return null;

        Transform view = head != null ? head : transform;
        Vector3 fwd = view.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
        fwd.Normalize();
        Vector3 pos = (rightHand != null ? rightHand.position : view.position - Vector3.up * 0.4f) + fwd * 0.3f;
        Quaternion rot = Quaternion.LookRotation(fwd);

        GameObject obj = kittyBlasterPrefab != null
            ? Instantiate(kittyBlasterPrefab, pos, rot)
            : KittyBlaster.Create(pos, rot);
        obj.name = "Kitty Blaster";

        // Float in the air until grabbed (grabbing + letting go turns normal physics back on).
        var body = obj.GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;
        FxSpawner.Confetti(pos, 15);
        KittySounds.Play(null, KittySounds.Mrrp, pos, 0.5f);
        return obj;
    }

    // ------------------------------------------------------------ coins

    public void GiveCoins(int amount)
    {
        if (!IsMod) return;
        CurrencyWallet.Add(amount);
    }

    // ------------------------------------------------------------ bans
    // NOTE: this ban list is only saved on THIS device. A real ban has to be enforced by a
    // multiplayer server (the server refuses banned players). Hook these up when multiplayer is added.

    public void BanPlayer(string playerId)
    {
        if (!IsMod || string.IsNullOrWhiteSpace(playerId)) return;
        var bans = GetBans();
        if (bans.Add(playerId)) SaveBans(bans);
        Debug.Log("[Mod] Banned " + playerId);
    }

    public void UnbanPlayer(string playerId)
    {
        if (!IsMod) return;
        var bans = GetBans();
        if (bans.Remove(playerId)) SaveBans(bans);
    }

    public static bool IsBanned(string playerId) => GetBans().Contains(playerId);

    static HashSet<string> GetBans()
    {
        string raw = PlayerPrefs.GetString(BanKey, "");
        return new HashSet<string>(raw.Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries));
    }

    static void SaveBans(HashSet<string> bans)
    {
        PlayerPrefs.SetString(BanKey, string.Join("|", bans));
        PlayerPrefs.Save();
    }
}
