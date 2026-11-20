using System.Collections.Generic;
using UnityEngine;

// A little black kitten fired by the Kitty Blaster.
//   1. Flies out of the barrel in an arc and lands.
//   2. Runs to the nearest target within 15 m (anything with Health: players, Training Dummies...),
//      but NEVER the mod who fired it, and never other kittens.
//   3. Hops on and scratches: 10 damage (Kitty Blaster's "Scratch Damage" setting) every 0.5 s for up to 3 s, with a tiny push.
//   4. No target? It wanders, sits and swishes its tail.
//   5. Poofs (smoke + confetti) after 8 s, after it's done scratching, or when any weapon hits it (it has 1 HP).
public class Kitten : MonoBehaviour
{
    enum State { Flying, Running, Wandering, Sitting, Hopping, Scratching }

    public const int MaxPerShooter = 15;
    const float LifeSeconds = 8f;
    const float SeekRadius = 15f;
    const float RunSpeed = 3.2f;
    const float WanderSpeed = 0.9f;
    public const float DefaultScratchDamage = 10f;
    const float ScratchEvery = 0.5f;
    const float ScratchSeconds = 3f;
    const float HopSeconds = 0.3f;
    const float SearchEvery = 0.5f;

    static readonly List<Kitten> alive = new List<Kitten>();
    static Health[] healthCache;
    static int healthCacheFrame = -1;
    static PhysicsMaterial slippery;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()   // fast Play mode keeps statics between runs
    {
        alive.Clear();
        healthCache = null;
        healthCacheFrame = -1;
    }

    static readonly Color Fur = new Color(0.07f, 0.07f, 0.09f);
    static readonly Color EyeYellow = new Color(1f, 0.88f, 0.15f);
    static readonly Color NosePink = new Color(1f, 0.55f, 0.7f);

    [Tooltip("Damage per scratch (one scratch every 0.5 s). Set by the Kitty Blaster that fired it.")]
    public float scratchDamage = DefaultScratchDamage;

    GameObject shooter;
    AudioClip hissClip;
    Rigidbody rb;
    Collider body;
    Transform visual, tail;

    State state;
    float bornAt, stateUntil, nextSearch, nextScratch, scratchEndsAt, hopStart, swipeAt = -10f;
    Health target;
    Collider targetCol;
    Vector3 hopFrom, attachLocal, faceLocal, wanderDir;
    bool poofed;

    // ------------------------------------------------------------ spawning

    public static Kitten Spawn(Vector3 position, Vector3 velocity, GameObject shooter, AudioClip hiss = null,
                               float scratchDamage = DefaultScratchDamage)
    {
        // Max 15 kittens per shooter: the oldest one poofs to make room.
        alive.RemoveAll(k => k == null);
        Kitten oldest = null;
        int count = 0;
        foreach (var k in alive)
        {
            if (k.shooter != shooter) continue;
            count++;
            if (oldest == null || k.bornAt < oldest.bornAt) oldest = k;
        }
        if (count >= MaxPerShooter && oldest != null) oldest.Poof();

        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        var go = Build(position, flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat) : Quaternion.identity);

        var kitten = go.AddComponent<Kitten>();
        kitten.Init(velocity, shooter, hiss, scratchDamage);
        alive.Add(kitten);
        return kitten;
    }

    void Init(Vector3 velocity, GameObject shooterPlayer, AudioClip hiss, float damage)
    {
        scratchDamage = damage;
        shooter = shooterPlayer;
        hissClip = hiss;
        rb = GetComponent<Rigidbody>();
        body = GetComponent<Collider>();
        visual = transform.Find("Visual");
        tail = visual != null ? visual.Find("Tail") : null;
        bornAt = Time.time;
        state = State.Flying;
        rb.linearVelocity = velocity;

        // 1 HP: any weapon hit poofs it.
        var health = gameObject.AddComponent<Health>();
        health.maxHealth = 1f;
        health.Revive();
        health.Died += Poof;

        // Don't bump into the mod who fired it.
        if (shooter != null)
            foreach (var c in shooter.GetComponentsInChildren<Collider>())
                if (c != null && c.enabled) Physics.IgnoreCollision(body, c);
    }

    void OnDestroy() => alive.Remove(this);

    // ------------------------------------------------------------ brain (physics steps)

    void FixedUpdate()
    {
        if (poofed) return;
        switch (state)
        {
            case State.Flying:
                if (Time.time - bornAt > 0.15f && rb.linearVelocity.y <= 0.5f && Grounded()) Land();
                break;

            case State.Running:
                RunToTarget();
                break;

            case State.Wandering:
                if (LookForTarget()) break;
                if (Time.time >= stateUntil) { state = State.Sitting; stateUntil = Time.time + Random.Range(1.5f, 3f); break; }
                Move(wanderDir, WanderSpeed);
                break;

            case State.Sitting:
                if (LookForTarget()) break;
                Move(Vector3.zero, 0f);
                if (Time.time >= stateUntil) StartWander();
                break;
        }
    }

    void Land()
    {
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        if (FindTarget()) state = State.Running;
        else StartWander();
    }

    void StartWander()
    {
        state = State.Wandering;
        Vector2 r = Random.insideUnitCircle.normalized;
        wanderDir = new Vector3(r.x, 0f, r.y);
        stateUntil = Time.time + Random.Range(0.8f, 1.8f);
    }

    // While idle, check for targets twice a second.
    bool LookForTarget()
    {
        if (Time.time < nextSearch) return false;
        nextSearch = Time.time + SearchEvery;
        if (!FindTarget()) return false;
        state = State.Running;
        return true;
    }

    void RunToTarget()
    {
        if (!IsValidTarget(target) || (target.transform.position - transform.position).sqrMagnitude > SeekRadius * SeekRadius * 1.5f)
        {
            if (!FindTarget()) { StartWander(); return; }
        }

        Bounds b = TargetBounds();
        Vector3 goal = b.ClosestPoint(transform.position + Vector3.up * 0.1f);
        Vector3 flat = goal - transform.position;
        flat.y = 0f;
        float dist = flat.magnitude;
        bool canReach = b.min.y - transform.position.y < 2f; // don't chase someone flying way up high

        if (dist < 0.35f)
        {
            if (canReach) StartHop();
            else Move(Vector3.zero, 0f);   // wait underneath
            return;
        }
        Move(flat / dist, RunSpeed);
    }

    void Move(Vector3 dir, float speed)
    {
        if (!Grounded()) return;   // falling - let gravity do its thing
        Vector3 v = rb.linearVelocity;
        v.x = dir.x * speed;
        v.z = dir.z * speed;
        rb.linearVelocity = v;
        if (dir.sqrMagnitude > 0.01f)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), 0.3f));
    }

    bool Grounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.06f, Vector3.down, out RaycastHit hit, 0.12f,
                               ~0, QueryTriggerInteraction.Ignore) && hit.collider != body;
    }

    // Landing right on a target? Start scratching straight away.
    void OnCollisionEnter(Collision collision)
    {
        if (poofed || (state != State.Flying && state != State.Running)) return;
        var h = collision.collider.GetComponentInParent<Health>();
        if (!IsValidTarget(h)) return;
        target = h;
        targetCol = collision.collider;
        StartHop();
    }

    // ------------------------------------------------------------ finding targets

    bool IsValidTarget(Health h)
    {
        if (h == null || !h.isActiveAndEnabled || h.IsDead) return false;
        if (h.GetComponent<Kitten>() != null) return false;               // not other kittens
        if (SpawnSafeZone.Protects(h)) return false;                      // not players in the spawn safe zone
                                                                           // (a scratching kitten poofs if they step in)
        if (shooter != null && (h.gameObject == shooter || h.transform.IsChildOf(shooter.transform)))
            return false;                                                   // NEVER the mod who fired it
        return true;
    }

    bool FindTarget()
    {
        if (healthCacheFrame != Time.frameCount)
        {
            healthCache = FindObjectsByType<Health>();
            healthCacheFrame = Time.frameCount;
        }

        Health best = null;
        float bestDist = SeekRadius * SeekRadius;
        foreach (var h in healthCache)
        {
            if (!IsValidTarget(h)) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = h; }
        }

        target = best;
        targetCol = best != null ? ColliderOf(best) : null;
        return best != null;
    }

    static Collider ColliderOf(Health h)
    {
        var c = h.GetComponent<Collider>();
        if (c != null && c.enabled && !c.isTrigger) return c;
        foreach (var child in h.GetComponentsInChildren<Collider>())
            if (child.enabled && !child.isTrigger) return child;
        return null;
    }

    Bounds TargetBounds()
    {
        if (targetCol != null && targetCol.enabled) return targetCol.bounds;
        return new Bounds(target.transform.position + Vector3.up * 0.8f, new Vector3(0.6f, 1.6f, 0.6f));
    }

    // ------------------------------------------------------------ hop on + scratch

    void StartHop()
    {
        state = State.Hopping;
        hopStart = Time.time;
        hopFrom = transform.position;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        body.enabled = false;

        // Pick a spot on the target's side, a bit above the middle, facing in.
        Bounds b = TargetBounds();
        Vector3 center = new Vector3(b.center.x, Mathf.Lerp(b.min.y, b.max.y, 0.55f), b.center.z);
        Vector3 outward = transform.position - center;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : -transform.forward;
        float radius = Mathf.Min(b.extents.x, b.extents.z);
        Vector3 cling = center + outward * (radius + 0.06f) - Vector3.up * 0.08f;

        attachLocal = target.transform.InverseTransformPoint(cling);
        faceLocal = target.transform.InverseTransformDirection(-outward);
    }

    void BeginScratching()
    {
        state = State.Scratching;
        scratchEndsAt = Time.time + ScratchSeconds;
        nextScratch = Time.time;
        KittySounds.Play(hissClip, KittySounds.Hiss, transform.position, 0.6f);
    }

    void Scratch()
    {
        nextScratch += ScratchEvery;
        swipeAt = Time.time;

        Vector3 dir = target.transform.TransformDirection(faceLocal);   // toward the target
        Vector3 point = transform.position + dir * 0.08f + Vector3.up * 0.1f;
        target.TakeDamage(scratchDamage, point, dir);

        // tiny push back (only moves things with physics, like players). No screen shake.
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude > 0.0001f) EffectSystem.Push(target.gameObject, flat.normalized * 0.4f);

        ScratchMarks(point, -dir);
        KittySounds.Play(null, KittySounds.Scratch, point, 0.4f);

        // Scratching the player wearing the headset? Show a few soft scratch marks at the edge of their view.
        var cam = Camera.main;
        if (cam != null && target.GetComponent<PlayerDeathPhysics>() != null && cam.transform.IsChildOf(target.transform))
            ScratchVision.Show();
    }

    // Three little white slashes that fly off and fade.
    static void ScratchMarks(Vector3 point, Vector3 outward)
    {
        if (outward.sqrMagnitude < 0.0001f) outward = Vector3.up;
        Quaternion look = Quaternion.LookRotation(outward) * Quaternion.Euler(0f, 0f, 35f);
        Vector3 side = look * Vector3.right;
        for (int i = 0; i < 3; i++)
        {
            var p = Puff.Create(PrimitiveType.Cube, point + side * (i - 1) * 0.025f, Color.white, 0.05f, 0.05f, 0.35f,
                                outward * 0.6f + Random.insideUnitSphere * 0.2f, false);
            p.keepShape = true;
            p.transform.rotation = look;
            p.transform.localScale = new Vector3(0.008f, 0.07f, 0.004f);
        }
        FxSpawner.Burst(point, Color.white, 3, 1.2f, 0.02f);
    }

    // ------------------------------------------------------------ every frame: hop, scratch, animate, lifetime

    void Update()
    {
        if (poofed) return;
        if (Time.time - bornAt > LifeSeconds || transform.position.y < -50f) { Poof(); return; }

        if (state == State.Hopping || state == State.Scratching)
        {
            if (!IsValidTarget(target)) { Poof(); return; }   // target got KO'd or vanished
            Vector3 dest = target.transform.TransformPoint(attachLocal);
            Vector3 face = target.transform.TransformDirection(faceLocal);
            face.y = 0f;
            if (face.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(face);

            if (state == State.Hopping)
            {
                float k = Mathf.Clamp01((Time.time - hopStart) / HopSeconds);
                transform.position = Vector3.Lerp(hopFrom, dest, k) + Vector3.up * Mathf.Sin(Mathf.PI * k) * 0.3f;
                if (k >= 1f) BeginScratching();
            }
            else
            {
                transform.position = dest;
                if (Time.time >= scratchEndsAt) { Poof(); return; }  // all done scratching
                if (Time.time >= nextScratch) Scratch();
            }
        }

        Animate();
    }

    void Animate()
    {
        if (visual == null) return;
        float t = Time.time + bornAt * 3.7f; // so kittens don't all move in sync

        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        float swishSpeed = 6f, swishAmount = 20f;

        switch (state)
        {
            case State.Flying:
                rot = Quaternion.Euler(-20f, 0f, 0f);                       // little "wheee" lean back
                swishSpeed = 14f; swishAmount = 30f;
                break;
            case State.Running:
                pos.y = Mathf.Abs(Mathf.Sin(t * 20f)) * 0.03f;              // bouncy run
                rot = Quaternion.Euler(Mathf.Sin(t * 20f) * 6f, 0f, 0f);
                swishSpeed = 12f; swishAmount = 12f;
                break;
            case State.Wandering:
                pos.y = Mathf.Abs(Mathf.Sin(t * 10f)) * 0.012f;
                break;
            case State.Sitting:
                rot = Quaternion.Euler(-18f, 0f, 0f);                       // sit back on its bottom
                swishSpeed = 3.5f; swishAmount = 35f;                        // slow, happy tail swish
                break;
            case State.Hopping:
                rot = Quaternion.Euler(-30f, 0f, 0f);
                break;
            case State.Scratching:
                float swipe = Mathf.Clamp01(1f - (Time.time - swipeAt) / 0.25f);
                rot = Quaternion.Euler(-35f + Mathf.Sin(t * 30f) * 10f * (0.4f + swipe), 0f, 0f); // scritch scritch
                swishSpeed = 16f; swishAmount = 25f;
                break;
        }
        visual.localPosition = pos;
        visual.localRotation = rot;
        if (tail != null)
            tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * swishSpeed) * swishAmount) * Quaternion.Euler(-35f, 0f, 0f);
    }

    // ------------------------------------------------------------ poof!

    public void Poof()
    {
        if (poofed) return;
        poofed = true;
        alive.Remove(this);

        Vector3 p = transform.position + Vector3.up * 0.08f;
        for (int i = 0; i < 5; i++)
        {
            float g = Random.Range(0.7f, 0.9f);
            Puff.Create(PrimitiveType.Sphere, p + Random.insideUnitSphere * 0.06f, new Color(g, g, g), 0.05f,
                        Random.Range(0.18f, 0.28f), 0.55f, Random.insideUnitSphere * 0.4f + Vector3.up * 0.3f, false);
        }
        FxSpawner.Confetti(p, 12);
        SoundFx.Play(Sfx.Poof, p, 0.35f);
        Destroy(gameObject);
    }

    // ------------------------------------------------------------ building one (from shapes)

    // About 0.2 m long. Forward (+Z) is where it looks. The origin is at its feet.
    static GameObject Build(Vector3 position, Quaternion rotation)
    {
        var root = new GameObject("Kitten");
        root.transform.SetPositionAndRotation(position, rotation);

        var v = new GameObject("Visual").transform;
        v.SetParent(root.transform, false);
        var S = PrimitiveType.Sphere;

        KittyLook.Part(v, S, "Body", new Vector3(0f, 0.065f, 0f), new Vector3(0.1f, 0.09f, 0.14f), Vector3.zero, Fur);
        KittyLook.Part(v, S, "Head", new Vector3(0f, 0.11f, 0.075f), Vector3.one * 0.095f, Vector3.zero, Fur);
        KittyLook.Part(v, S, "Nose", new Vector3(0f, 0.103f, 0.122f), Vector3.one * 0.012f, Vector3.zero, NosePink);
        for (int side = -1; side <= 1; side += 2)
        {
            // triangle ears: a turned cube, half hidden in the head
            KittyLook.Part(v, PrimitiveType.Cube, "Ear", new Vector3(side * 0.027f, 0.152f, 0.072f),
                           new Vector3(0.03f, 0.03f, 0.008f), new Vector3(0f, 0f, 45f), Fur);
            // glowing yellow eyes
            var eye = KittyLook.Part(v, S, "Eye", new Vector3(side * 0.022f, 0.118f, 0.114f), Vector3.one * 0.022f, Vector3.zero, EyeYellow);
            KittyLook.MakeGlow(eye, EyeYellow);
            // little feet
            KittyLook.Part(v, S, "Foot", new Vector3(side * 0.03f, 0.015f, 0.045f), Vector3.one * 0.03f, Vector3.zero, Fur);
            KittyLook.Part(v, S, "Foot", new Vector3(side * 0.03f, 0.015f, -0.045f), Vector3.one * 0.03f, Vector3.zero, Fur);
        }

        var tail = new GameObject("Tail").transform;
        tail.SetParent(v, false);
        tail.localPosition = new Vector3(0f, 0.08f, -0.065f);
        tail.localRotation = Quaternion.Euler(-35f, 0f, 0f);
        KittyLook.Part(tail, PrimitiveType.Cylinder, "Tail", new Vector3(0f, 0.045f, 0f), new Vector3(0.016f, 0.045f, 0.016f), Vector3.zero, Fur);
        KittyLook.Part(tail, S, "Tail Tip", new Vector3(0f, 0.09f, 0f), Vector3.one * 0.02f, Vector3.zero, Fur);

        var col = root.AddComponent<SphereCollider>();
        col.center = new Vector3(0f, 0.07f, 0f);
        col.radius = 0.07f;
        col.sharedMaterial = Slippery;

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return root;
    }

    static PhysicsMaterial Slippery
    {
        get
        {
            if (slippery == null)
                slippery = new PhysicsMaterial("Kitten Paws")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum
                };
            return slippery;
        }
    }
}
