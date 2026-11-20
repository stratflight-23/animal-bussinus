using TMPro;
using UnityEngine;
using UnityEngine.XR;

// The Goop Copter: a cartoon barrel helicopter. Build one with Gorilla Company > 6. Add Goop Copter.
//   Get in : stand next to it and grab one of the red lever knobs (grip)
//   Get out: B or Y button
//   Left stick = fly forward/back/sideways   Right stick left/right = turn   Right stick up/down = up/down
//   Flat-screen testing: F = get in/out, WASD = fly, Q/E = turn, Space/Left Ctrl = up/down
// VR comfort: you sit on a part that only turns left/right. The barrel tilts a little, your view never does.
[RequireComponent(typeof(Rigidbody))]
public class GoopCopter : MonoBehaviour
{
    [Header("Parts (filled in by the builder)")]
    public Transform seat;        // the player sits here (only turns left/right, never tilts)
    public Transform body;        // the barrel and everything that tilts
    public Transform mainRotor;
    public Transform tailRotor;
    public TextMeshPro dashboard;

    [Header("Flying")]
    public float maxSpeed = 7f;
    public float climbSpeed = 3.5f;
    [Tooltip("Degrees per second.")]
    public float turnSpeed = 60f;
    [Tooltip("Gentle = comfortable in VR.")]
    public float acceleration = 3f;
    public float maxTilt = 8f;
    [Tooltip("When out of fuel, it won't fall faster than this (soft landing).")]
    public float maxFallSpeed = 3.5f;
    public float stickDeadzone = 0.15f;

    [Header("Fuel")]
    public float maxFuel = 100f;
    [Tooltip("Fuel used per second while in the air.")]
    public float fuelPerSecond = 1.2f;
    [Tooltip("Fuel gained per second while sitting on the ground.")]
    public float refuelPerSecond = 4f;

    [Header("Sound")]
    [Tooltip("Optional. If empty, a built-in whirring sound is used.")]
    public AudioClip engineSound;

    public float Fuel { get; private set; }
    public bool HasPilot => pilot != null;
    public float Height { get; private set; }
    public bool Grounded { get; private set; }

    Rigidbody rb;
    AudioSource engineAudio;
    Collider[] myColliders;
    readonly RaycastHit[] hits = new RaycastHit[16];

    GameObject pilot;
    Rigidbody pilotRb;
    gorillalocomotion pilotLoco;
    PlayerDeathPhysics pilotDeath;
    RigidbodyInterpolation pilotInterpolation;
    Collider[] pilotColliders;
    float enteredAt;

    Vector2 moveInput;
    float turnInput, climbInput;
    bool exitWasPressed;
    float rotorSpeed; // 0..1

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;          // we turn it ourselves, it never tips over
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        Fuel = maxFuel;
        myColliders = GetComponentsInChildren<Collider>(true);

        foreach (var handle in GetComponentsInChildren<GripHandle>(true))
            handle.Gripped += OnLeverGripped;

        engineAudio = gameObject.AddComponent<AudioSource>();
        engineAudio.clip = engineSound != null ? engineSound : SoundFx.CopterLoop;
        engineAudio.loop = true;
        engineAudio.spatialBlend = 1f;
        engineAudio.minDistance = 2f;
        engineAudio.maxDistance = 40f;
        engineAudio.volume = 0f;
        engineAudio.Play();
    }

    // ------------------------------------------------------------ getting in and out

    void OnLeverGripped(HandGrabber hand)
    {
        if (HasPilot || hand.PlayerRoot == null) return;
        Vector3 d = hand.PlayerRoot.transform.position - transform.position;
        d.y = 0f;
        if (d.magnitude > 2.5f) return; // must be standing next to it
        Enter(hand.PlayerRoot);
    }

    public void Enter(GameObject player)
    {
        if (HasPilot || player == null) return;
        pilot = player;
        pilotRb = player.GetComponent<Rigidbody>();
        pilotLoco = player.GetComponent<gorillalocomotion>();
        pilotDeath = player.GetComponent<PlayerDeathPhysics>();

        if (pilotLoco != null) pilotLoco.enabled = false;   // no walking while seated
        if (pilotRb != null)
        {
            pilotInterpolation = pilotRb.interpolation;
            pilotRb.linearVelocity = Vector3.zero;
            pilotRb.isKinematic = true;
            pilotRb.interpolation = RigidbodyInterpolation.None;
        }

        pilotColliders = player.GetComponentsInChildren<Collider>(true);
        IgnorePilot(true);

        player.transform.SetParent(seat, false);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        enteredAt = Time.time;
        exitWasPressed = true; // ignore a button that was already held
        SoundFx.Play(Sfx.Bonk, transform.position, 0.5f);
    }

    public void Exit()
    {
        if (!HasPilot) return;
        var player = pilot;
        pilot = null;

        player.transform.SetParent(null, true);
        Vector3 outPos = transform.position + transform.right * 1.8f;
        if (Physics.Raycast(outPos + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
            outPos.y = hit.point.y + 0.05f;
        player.transform.SetPositionAndRotation(outPos, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

        IgnorePilot(false);
        if (pilotRb != null)
        {
            pilotRb.isKinematic = false;
            pilotRb.interpolation = pilotInterpolation;
            pilotRb.position = outPos;
            pilotRb.linearVelocity = rb.linearVelocity;
        }
        bool dead = pilotDeath != null && pilotDeath.IsDead;
        if (pilotLoco != null && !dead) pilotLoco.enabled = true;
    }

    void IgnorePilot(bool ignore)
    {
        if (pilotColliders == null) return;
        foreach (var mine in myColliders)
            foreach (var theirs in pilotColliders)
                if (mine != null && theirs != null && mine.enabled && theirs.enabled)
                    Physics.IgnoreCollision(mine, theirs, ignore);
    }

    // ------------------------------------------------------------ input (every frame)

    void Update()
    {
        ReadInput();
        UpdateVisuals();
    }

    void ReadInput()
    {
        moveInput = Vector2.zero;
        turnInput = 0f;
        climbInput = 0f;

        var kb = UnityEngine.InputSystem.Keyboard.current;

        if (!HasPilot)
        {
            // flat-screen testing: F next to the copter gets you in
            if (kb != null && kb.fKey.wasPressedThisFrame)
            {
                var player = GameObject.Find("GorillaPlayer");
                if (player != null && Vector3.Distance(player.transform.position, transform.position) < 3.5f) Enter(player);
            }
            return;
        }

        if (pilotDeath != null && pilotDeath.IsDead) { Exit(); return; }

        if (VRInput.Stick(XRNode.LeftHand, out Vector2 l) && l.magnitude > stickDeadzone) moveInput = l;
        if (VRInput.Stick(XRNode.RightHand, out Vector2 r))
        {
            if (Mathf.Abs(r.x) > stickDeadzone) turnInput = r.x;
            if (Mathf.Abs(r.y) > stickDeadzone) climbInput = r.y;
        }
        bool exit = VRInput.Secondary(XRNode.RightHand) || VRInput.Secondary(XRNode.LeftHand);

        if (kb != null)
        {
            if (kb.wKey.isPressed) moveInput.y += 1f;
            if (kb.sKey.isPressed) moveInput.y -= 1f;
            if (kb.dKey.isPressed) moveInput.x += 1f;
            if (kb.aKey.isPressed) moveInput.x -= 1f;
            if (kb.eKey.isPressed) turnInput += 1f;
            if (kb.qKey.isPressed) turnInput -= 1f;
            if (kb.spaceKey.isPressed) climbInput += 1f;
            if (kb.leftCtrlKey.isPressed) climbInput -= 1f;
            if (kb.fKey.wasPressedThisFrame) exit = true;
        }
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        turnInput = Mathf.Clamp(turnInput, -1f, 1f);
        climbInput = Mathf.Clamp(climbInput, -1f, 1f);

        if (exit && !exitWasPressed && Time.time - enteredAt > 0.5f) Exit();
        exitWasPressed = exit;
    }

    // ------------------------------------------------------------ physics

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        CheckGround();
        bool engineOn = HasPilot && Fuel > 0f;

        if (engineOn)
        {
            Quaternion yaw = Quaternion.Euler(0f, rb.rotation.eulerAngles.y, 0f);
            Vector3 desired = yaw * new Vector3(moveInput.x, 0f, moveInput.y) * maxSpeed;
            desired.y = climbInput * climbSpeed;

            bool resting = Grounded && climbInput <= 0.05f;
            if (resting) desired = new Vector3(0f, rb.linearVelocity.y, 0f); // parked: no sliding around
            rb.useGravity = resting;

            Vector3 v = rb.linearVelocity;
            Vector3 target = resting ? new Vector3(0f, v.y, 0f) : desired;
            rb.linearVelocity = Vector3.MoveTowards(v, target, acceleration * dt);

            if (Mathf.Abs(turnInput) > 0f)
                rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnInput * turnSpeed * dt, 0f));

            if (!Grounded)
            {
                float throttle = Mathf.Clamp01(moveInput.magnitude + Mathf.Abs(climbInput));
                Fuel = Mathf.Max(0f, Fuel - fuelPerSecond * (0.6f + 0.4f * throttle) * dt);
            }
        }
        else
        {
            // No pilot or no fuel: gravity takes over, but the spinning blades make it a soft fall.
            rb.useGravity = true;
            Vector3 v = rb.linearVelocity;
            if (!Grounded)
            {
                if (v.y < -maxFallSpeed) v.y = -maxFallSpeed;
                v.x *= 1f - 0.5f * dt;
                v.z *= 1f - 0.5f * dt;
            }
            else
            {
                v.x *= 1f - 5f * dt;
                v.z *= 1f - 5f * dt;
            }
            rb.linearVelocity = v;
        }

        if (Grounded) Fuel = Mathf.Min(maxFuel, Fuel + refuelPerSecond * dt);
    }

    void CheckGround()
    {
        Vector3 from = rb.position + Vector3.up * 0.5f;
        int n = Physics.RaycastNonAlloc(from, Vector3.down, hits, 500f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var col = hits[i].collider;
            if (col.transform.IsChildOf(transform)) continue;
            if (pilot != null && col.transform.IsChildOf(pilot.transform)) continue;
            if (hits[i].distance < best) best = hits[i].distance;
        }
        Height = best == float.MaxValue ? 999f : best - 0.5f;
        Grounded = Height < 0.12f;
    }

    // ------------------------------------------------------------ visuals and sound

    void UpdateVisuals()
    {
        float dt = Time.deltaTime;
        bool engineOn = HasPilot && Fuel > 0f;
        float target = engineOn ? 0.55f + 0.45f * Mathf.Clamp01(moveInput.magnitude + Mathf.Abs(climbInput))
                     : (!Grounded ? 0.3f : 0f); // blades keep turning while it floats down
        rotorSpeed = Mathf.MoveTowards(rotorSpeed, target, dt * 0.8f);

        if (mainRotor != null) mainRotor.Rotate(0f, rotorSpeed * 1080f * dt, 0f, Space.Self);
        if (tailRotor != null) tailRotor.Rotate(rotorSpeed * 1800f * dt, 0f, 0f, Space.Self);

        if (engineAudio != null)
        {
            engineAudio.volume = rotorSpeed * 0.6f;
            engineAudio.pitch = 0.6f + rotorSpeed * 0.8f;
        }

        // Tilt only the barrel a little in the direction you're flying. The seat (and your view) stays level.
        if (body != null)
        {
            Vector3 local = Quaternion.Inverse(Quaternion.Euler(0f, transform.eulerAngles.y, 0f)) * rb.linearVelocity;
            Quaternion tilt = Quaternion.Euler(Mathf.Clamp(local.z / maxSpeed, -1f, 1f) * maxTilt, 0f,
                                               -Mathf.Clamp(local.x / maxSpeed, -1f, 1f) * maxTilt);
            body.localRotation = Quaternion.Slerp(body.localRotation, tilt, dt * 3f);
        }

        if (dashboard != null)
        {
            float pct = maxFuel > 0f ? Fuel / maxFuel : 0f;
            int bars = Mathf.RoundToInt(pct * 10f);
            string color = pct > 0.5f ? "#5CFF6A" : pct > 0.2f ? "#FFD84D" : "#FF4D4D";
            string fuelLine = Fuel <= 0f
                ? (Mathf.Repeat(Time.time, 1f) < 0.5f ? "<color=#FF4D4D>OUT OF FUEL</color>" : "")
                : $"FUEL <color={color}>{new string('|', bars)}<alpha=#33>{new string('|', 10 - bars)}</color><alpha=#FF> {Mathf.RoundToInt(pct * 100f)}%";
            string height = Height > 500f ? "--" : Height.ToString("0.0");
            string status = !HasPilot ? "Grab a lever to fly" : Grounded ? "Right stick up = take off" : "B / Y = get out";
            dashboard.text = $"{fuelLine}\nHEIGHT {height} m\n<size=60%>{status}</size>";
        }
    }
}
