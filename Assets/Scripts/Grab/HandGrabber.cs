using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

// Put this on each VR hand ("Left Hand" / "Right Hand" under GorillaPlayer). It's added automatically
// when the game starts, so you don't have to.
//   Grip near an item      = pick it up        Let go of grip = drop / throw it
//   Trigger while holding  = use the item (swing, shoot, place trap...)
// If the hand isn't touching an item, grip is left alone so gorillalocomotion can grab walls like before.
public class HandGrabber : MonoBehaviour
{
    [Tooltip("How close (meters) the hand must be to an item to grab it.")]
    public float grabRadius = 0.1f;
    [Tooltip("Throws are this much stronger than your real hand speed (feels better in VR).")]
    public float throwBoost = 1.3f;

    public XRNode Node { get; private set; }
    public Grabbable Held { get; private set; }
    public GameObject PlayerRoot { get; private set; }

    // True when this hand's grip is being used for an item or handle (so it shouldn't grab walls).
    public bool Busy => Held != null || hoverItem != null || hoverHandle != null || heldHandle != null;

    public Vector3 Velocity => velocity;

    static readonly Dictionary<XRNode, HandGrabber> hands = new Dictionary<XRNode, HandGrabber>();

    public static bool IsBusy(XRNode node) =>
        hands.TryGetValue(node, out var h) && h != null && h.isActiveAndEnabled && h.Busy;

    public static HandGrabber Get(XRNode node) =>
        hands.TryGetValue(node, out var h) ? h : null;

    Grabbable hoverItem;
    GripHandle hoverHandle;
    GripHandle heldHandle;
    bool gripWas, triggerWas;
    Transform attachParent;
    readonly Collider[] overlap = new Collider[32];

    // hand speed tracking (for throwing)
    const int HistorySize = 6;
    readonly Vector3[] posHistory = new Vector3[HistorySize];
    readonly float[] timeHistory = new float[HistorySize];
    int historyIndex;
    Vector3 velocity, angularVelocity;
    Quaternion lastRotation;

    void Awake()
    {
        Node = name.ToLower().Contains("left") ? XRNode.LeftHand : XRNode.RightHand;
        var body = GetComponentInParent<Rigidbody>();
        PlayerRoot = body != null ? body.gameObject : transform.root.gameObject;
        // Hands are scaled-down spheres, so held items ride on the hand's (unscaled) parent instead.
        attachParent = transform.parent != null ? transform.parent : transform;
        lastRotation = transform.rotation;
        for (int i = 0; i < HistorySize; i++) { posHistory[i] = transform.position; timeHistory[i] = Time.time; }
    }

    void OnEnable()
    {
        hands[Node] = this;
        Application.onBeforeRender += UpdateHeldPose;
    }

    void OnDisable()
    {
        Application.onBeforeRender -= UpdateHeldPose;
        if (hands.TryGetValue(Node, out var h) && h == this) hands.Remove(Node);
        if (Held != null) Release(false);
    }

    void Update()
    {
        TrackVelocity();

        bool grip = VRInput.Grip(Node);
        bool trigger = VRInput.Trigger(Node);

        FindHover();

        if (grip && !gripWas)
        {
            if (hoverItem != null) Grab(hoverItem);
            else if (hoverHandle != null)
            {
                heldHandle = hoverHandle;
                heldHandle.NotifyGripped(this);
                Buzz(0.3f, 0.05f);
            }
        }
        else if (!grip && gripWas)
        {
            if (Held != null) Release(true);
            if (heldHandle != null)
            {
                heldHandle.NotifyReleased(this);
                heldHandle = null;
            }
        }

        if (Held != null)
        {
            if (trigger && !triggerWas) Held.FireTriggerPressed(this);
            else if (trigger) Held.FireTriggerHeld(this);
        }

        gripWas = grip;
        triggerWas = trigger;
    }

    void LateUpdate() => UpdateHeldPose();

    void FindHover()
    {
        hoverItem = null;
        hoverHandle = null;
        if (Held != null) return;

        int n = Physics.OverlapSphereNonAlloc(transform.position, grabRadius, overlap, ~0, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var col = overlap[i];
            var item = col.GetComponentInParent<Grabbable>();
            if (item != null && !item.IsHeld)
            {
                float d = (col.ClosestPointOnBounds(transform.position) - transform.position).sqrMagnitude;
                if (d < best) { best = d; hoverItem = item; }
                continue;
            }
            if (hoverHandle == null)
            {
                var handle = col.GetComponentInParent<GripHandle>();
                if (handle != null) hoverHandle = handle;
            }
        }
        if (hoverItem != null) hoverHandle = null; // items win over handles
    }

    public void Grab(Grabbable item)
    {
        if (item.IsHeld) item.Holder.Release(false);
        Held = item;
        item.OnGrab(this);
        item.transform.SetParent(attachParent, true);
        UpdateHeldPose();
        Buzz(0.3f, 0.05f);
    }

    public void Release(bool throwIt)
    {
        var item = Held;
        if (item == null) return;
        Held = null;
        Vector3 v = throwIt ? velocity * throwBoost : Vector3.zero;
        Vector3 w = throwIt ? angularVelocity : Vector3.zero;
        item.OnRelease(this, v, w);
    }

    // Called when a held item gets destroyed (like a used-up Healing Mango).
    public void ForgetItem(Grabbable item)
    {
        if (Held == item) Held = null;
    }

    void UpdateHeldPose()
    {
        if (Held == null) return;
        Quaternion rot = transform.rotation * Quaternion.Euler(Held.gripRotationOffset);
        Held.transform.SetPositionAndRotation(transform.position + rot * Held.gripPositionOffset, rot);
    }

    void TrackVelocity()
    {
        float now = Time.time;
        historyIndex = (historyIndex + 1) % HistorySize;
        posHistory[historyIndex] = transform.position;
        timeHistory[historyIndex] = now;

        int oldest = (historyIndex + 1) % HistorySize;
        float dt = now - timeHistory[oldest];
        if (dt > 0.0001f)
            velocity = (posHistory[historyIndex] - posHistory[oldest]) / dt;

        float frameDt = Mathf.Max(Time.deltaTime, 0.0001f);
        Quaternion delta = transform.rotation * Quaternion.Inverse(lastRotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        if (!float.IsNaN(axis.x) && !float.IsInfinity(axis.x))
            angularVelocity = Vector3.Lerp(angularVelocity, axis * (angle * Mathf.Deg2Rad / frameDt), 0.5f);
        lastRotation = transform.rotation;
    }

    public void Buzz(float strength = 0.4f, float seconds = 0.05f)
    {
        VRInput.Buzz(Node, strength, seconds);
    }
}

// Adds the grab/health scripts to GorillaPlayer automatically when a scene starts,
// so older scenes work without re-running the setup menus.
static class GorillaRigBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Setup();
        SceneManager.sceneLoaded -= OnSceneLoaded;   // fast Play mode: don't add it twice
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Setup();

    public static void Setup()
    {
        var player = GameObject.Find("GorillaPlayer");
        if (player == null) return;

        if (player.GetComponent<Health>() == null) player.AddComponent<Health>();
        if (player.GetComponent<StatusEffects>() == null) player.AddComponent<StatusEffects>();
        if (player.GetComponent<PlayerHealthHud>() == null) player.AddComponent<PlayerHealthHud>();
        if (player.GetComponent<PlayerAvatar>() == null) player.AddComponent<PlayerAvatar>();   // body + cosmetics
        if (player.GetComponent<PlayerPets>() == null) player.AddComponent<PlayerPets>();       // your pet
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            var cam = player.GetComponentInChildren<Camera>();
            if (cam != null && cam.GetComponent<WebXRHeadFollower>() == null) cam.gameObject.AddComponent<WebXRHeadFollower>();
        }

        foreach (var t in player.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Left Hand" && t.name != "Right Hand") continue;
            if (t.GetComponent<HandGrabber>() == null) t.gameObject.AddComponent<HandGrabber>();
            if (t.GetComponent<HandPoker>() == null) t.gameObject.AddComponent<HandPoker>();   // press buttons, pet pets
            if (Application.platform == RuntimePlatform.WebGLPlayer && t.GetComponent<WebXRHandFollower>() == null)
                t.gameObject.AddComponent<WebXRHandFollower>();                                  // Quest browser version
        }
    }
}
