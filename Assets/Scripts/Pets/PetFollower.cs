using System.Collections.Generic;
using UnityEngine;

// A pet that follows its owner around. Made from the pet's shape recipe when it's summoned.
//   - Walks about 1 m behind you, off to the side. Hops up ledges, falls down drops.
//   - Flying pets float near your shoulder instead.
//   - More than 15 m away, or stuck for a few seconds? It pops right next to you.
//   - It has NO solid collider, so it can never block or push you. It has no Health, so nothing can hurt it,
//     and kittens / enemies / lava / spikes ignore it.
//   - Stand still and it does its favorite trick (sit / spin / hop / wag).
//   - Touch it with your hand to pet it: a heart pops up and your controller buzzes.
public class PetFollower : MonoBehaviour
{
    public PetData data;
    public Transform owner;

    const float TeleportDistance = 15f;
    const float MaxStepUp = 1.3f;     // highest ledge it can hop onto
    const float Gravity = 12f;

    Transform visual, tail;
    readonly List<Transform> wings = new List<Transform>();
    readonly List<Quaternion> wingBase = new List<Quaternion>();
    Rigidbody ownerBody;
    Transform ownerHead;
    float side = 1f;
    float animSeed;             // so two pets don't move in perfect sync

    bool moving;
    float vy;                   // falling speed
    bool hopping;
    float hopStart, hopFromY, hopToY;
    float stuckTimer, lastDist;
    float idleTimer, trickStart = -10f, nextPet;
    PetTrick currentTrick;
    float ownerStillTime;
    Vector3 lastOwnerPos;
    readonly RaycastHit[] hits = new RaycastHit[16];

    // ------------------------------------------------------------ making one

    public static PetFollower Spawn(PetData data, Transform owner)
    {
        var root = new GameObject("Pet - " + data.displayName);
        var pet = root.AddComponent<PetFollower>();
        pet.data = data;
        pet.owner = owner;
        pet.BuildLook();

        // A trigger (not solid) so hands can pet it. The kinematic Rigidbody just lets the trigger move cheaply.
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.25f;
        col.center = new Vector3(0f, 0.18f, 0f);
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        pet.TeleportNearOwner();
        return pet;
    }

    public static Transform BuildVisual(PetData data, Transform parent, out Transform tail, List<Transform> wings)
    {
        var visual = new GameObject("Visual").transform;
        visual.SetParent(parent, false);
        visual.localScale = Vector3.one * Mathf.Max(0.05f, data.size);

        tail = new GameObject("Tail Pivot").transform;
        tail.SetParent(visual, false);
        tail.localPosition = data.tailPivot;

        foreach (var go in PartRecipe.Build(data.parts, visual))
        {
            if (go.name == "Tail")
                go.transform.SetParent(tail, true);       // keeps its spot, but now swings with the pivot
            else if (go.name == "Wing" && wings != null)
                wings.Add(go.transform);
        }
        return visual;
    }

    void BuildLook()
    {
        visual = BuildVisual(data, transform, out tail, wings);
        foreach (var w in wings) wingBase.Add(w.localRotation);   // flapping adds on top of how the wing is set
    }

    void Start()
    {
        if (owner != null)
        {
            ownerBody = owner.GetComponent<Rigidbody>();
            var cam = owner.GetComponentInChildren<Camera>();
            ownerHead = cam != null ? cam.transform : owner;
            lastOwnerPos = owner.position;
        }
        side = Random.value < 0.5f ? -1f : 1f;
        animSeed = Random.Range(0f, 100f);
    }

    // ------------------------------------------------------------ following

    void Update()
    {
        if (owner == null) { Destroy(gameObject); return; }
        float dt = Time.deltaTime;

        // is the owner standing still?
        float ownerSpeed = (owner.position - lastOwnerPos).magnitude / Mathf.Max(dt, 0.0001f);
        lastOwnerPos = owner.position;
        ownerStillTime = ownerSpeed < 0.3f ? ownerStillTime + dt : 0f;

        Quaternion ownerYaw = OwnerYaw();
        Vector3 target = owner.position + ownerYaw * new Vector3(0.7f * side, 0f, -1f);
        if (data.flies)
            target = ownerHead.position + ownerYaw * new Vector3(0.55f * side, -0.35f, -0.5f) + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.08f;

        Vector3 toTarget = target - transform.position;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        float dist = flat.magnitude;

        if ((owner.position - transform.position).magnitude > TeleportDistance || stuckTimer > 2.5f)
        {
            TeleportNearOwner();
            return;
        }

        if (data.flies) Fly(toTarget, dt);
        else Walk(flat, dist, dt);

        // face where we're going, or the owner when idle
        Vector3 look = moving ? flat : (owner.position - transform.position);
        look.y = 0f;
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 8f * dt);

        Animate(dt);
    }

    void Walk(Vector3 flat, float dist, float dt)
    {
        if (!moving && dist > 0.9f) moving = true;
        if (moving && dist < 0.35f) moving = false;

        float groundHere = GroundY(transform.position, transform.position.y);
        if (moving)
        {
            float speed = data.followSpeed * Mathf.Clamp(dist / 2f, 0.6f, 2f);   // hurry up when far behind
            Vector3 step = flat / dist * Mathf.Min(speed * dt, dist);
            Vector3 next = transform.position + step;
            float groundNext = GroundY(next + flat / dist * 0.15f, transform.position.y);

            if (groundNext - groundHere > MaxStepUp)
            {
                stuckTimer += dt;            // a wall: wait (after a few seconds it pops over to you)
            }
            else
            {
                transform.position = new Vector3(next.x, transform.position.y, next.z);
                if (!hopping && groundNext - transform.position.y > 0.2f) StartHop(groundNext);   // hop up the ledge
                stuckTimer = dist > lastDist - 0.002f && dist > 2f ? stuckTimer + dt * 0.5f : Mathf.Max(0f, stuckTimer - dt);
            }
            groundHere = GroundY(transform.position, transform.position.y);
        }
        else stuckTimer = 0f;
        lastDist = dist;

        // up and down: hopping, falling, or standing on the ground
        Vector3 p = transform.position;
        if (hopping)
        {
            float k = Mathf.Clamp01((Time.time - hopStart) / 0.35f);
            p.y = Mathf.Lerp(hopFromY, hopToY, k) + Mathf.Sin(Mathf.PI * k) * 0.35f;
            if (k >= 1f) { hopping = false; vy = 0f; }
        }
        else if (p.y > groundHere + 0.01f)
        {
            vy -= Gravity * dt;
            p.y = Mathf.Max(groundHere, p.y + vy * dt);
            if (p.y <= groundHere) vy = 0f;
        }
        else
        {
            p.y = groundHere;
            vy = 0f;
        }
        transform.position = p;
    }

    void Fly(Vector3 toTarget, float dt)
    {
        float d = toTarget.magnitude;
        moving = d > 0.25f;
        float speed = data.followSpeed * Mathf.Clamp(d, 0.5f, 3f);
        Vector3 p = transform.position + toTarget.normalized * Mathf.Min(speed * dt, d);
        float ground = GroundY(p, p.y);
        if (p.y < ground + 0.3f) p.y = ground + 0.3f;
        transform.position = p;
        stuckTimer = 0f;
    }

    void StartHop(float toY)
    {
        hopping = true;
        hopStart = Time.time;
        hopFromY = transform.position.y;
        hopToY = toY;
    }

    public void TeleportNearOwner()
    {
        if (owner == null) return;
        Vector3 p = owner.position + OwnerYaw() * new Vector3(0.7f * side, 0f, -1f);
        float ground = GroundY(p, owner.position.y + 0.5f);
        p.y = data != null && data.flies ? owner.position.y + 1.3f : ground;
        transform.position = p;
        hopping = false;
        vy = 0f;
        stuckTimer = 0f;
        FxSpawner.Burst(p + Vector3.up * 0.2f, Color.white, 6, 1.5f, 0.05f);
    }

    Quaternion OwnerYaw()
    {
        Transform t = ownerHead != null ? ownerHead : owner;
        Vector3 f = t.forward;
        f.y = 0f;
        return Quaternion.LookRotation(f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward);
    }

    // Height of the floor under a point (ignores the owner, loose physics items and all triggers).
    float GroundY(Vector3 p, float fallback)
    {
        Vector3 from = new Vector3(p.x, Mathf.Max(p.y, fallback) + MaxStepUp + 0.3f, p.z);
        int n = Physics.RaycastNonAlloc(from, Vector3.down, hits, 40f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            var body = hits[i].rigidbody;
            if (body != null && (body == ownerBody || !body.isKinematic)) continue;
            if (hits[i].point.y > best) best = hits[i].point.y;
        }
        return float.IsNegativeInfinity(best) ? fallback : best;
    }

    // ------------------------------------------------------------ cute animations

    void Animate(float dt)
    {
        if (visual == null) return;
        float t = Time.time + animSeed;
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        float wag = Mathf.Sin(t * 5f) * 15f;
        Vector3 squash = Vector3.one;

        bool idle = !moving && !hopping && ownerStillTime > 1.5f;
        if (idle)
        {
            idleTimer += dt;
            if (idleTimer > 3.5f) { idleTimer = 0f; trickStart = Time.time; currentTrick = data.idleTrick; }
        }
        else idleTimer = 0f;

        float tk = (Time.time - trickStart) / 1.4f;
        if (tk >= 0f && tk < 1f && !moving)
        {
            switch (currentTrick)
            {
                case PetTrick.Sit: rot = Quaternion.Euler(-28f * Mathf.Sin(Mathf.PI * tk), 0f, 0f); break;
                case PetTrick.Spin: rot = Quaternion.Euler(0f, 360f * Mathf.SmoothStep(0f, 1f, tk), 0f); break;
                case PetTrick.Hop: pos.y = Mathf.Abs(Mathf.Sin(tk * Mathf.PI * 3f)) * 0.18f; break;
                case PetTrick.Wag: wag = Mathf.Sin(t * 22f) * 35f; break;
            }
        }
        else if (moving && !data.flies)
        {
            pos.y = Mathf.Abs(Mathf.Sin(t * 14f)) * (data.bouncy ? 0.12f : 0.03f);   // trotting bob
            wag = Mathf.Sin(t * 12f) * 20f;
        }

        if (data.bouncy)   // slime squish
        {
            float s = Mathf.Sin(t * (moving ? 14f : 4f)) * (moving ? 0.12f : 0.05f);
            squash = new Vector3(1f + s, 1f - s, 1f + s);
        }

        visual.localPosition = pos;
        visual.localRotation = rot;
        visual.localScale = Vector3.Scale(Vector3.one * data.size, squash);
        if (tail != null) tail.localRotation = Quaternion.Euler(0f, wag, 0f);
        float flap = data.flies ? Mathf.Sin(t * 26f) * 35f : 0f;
        for (int i = 0; i < wings.Count; i++)
            if (wings[i] != null) wings[i].localRotation = wingBase[i] * Quaternion.Euler(0f, 0f, (i % 2 == 0 ? flap : -flap));
    }

    // ------------------------------------------------------------ petting

    void OnTriggerEnter(Collider other)
    {
        var hand = other.GetComponent<HandPoker>();
        if (hand == null || Time.time < nextPet) return;
        nextPet = Time.time + 0.6f;
        hand.Buzz(0.35f, 0.08f);
        HeartPop.Spawn(transform.position + Vector3.up * (0.45f * data.size));
        SoundFx.Play(Sfx.Chime, transform.position, 0.25f);
        trickStart = Time.time;
        currentTrick = PetTrick.Hop;   // a happy little hop
    }
}

// A little pink heart (two balls + a turned cube) that floats up and shrinks away.
public class HeartPop : MonoBehaviour
{
    float age;

    public static void Spawn(Vector3 position)
    {
        var root = new GameObject("Heart");
        root.transform.position = position;
        Color pink = new Color(1f, 0.35f, 0.55f);
        var a = PrimitiveColor.MakeShape(PrimitiveType.Sphere, pink, "Heart");
        var b = PrimitiveColor.MakeShape(PrimitiveType.Sphere, pink, "Heart");
        var c = PrimitiveColor.MakeShape(PrimitiveType.Cube, pink, "Heart");
        a.transform.SetParent(root.transform, false); a.transform.localPosition = new Vector3(-0.03f, 0.02f, 0f); a.transform.localScale = Vector3.one * 0.07f;
        b.transform.SetParent(root.transform, false); b.transform.localPosition = new Vector3(0.03f, 0.02f, 0f); b.transform.localScale = Vector3.one * 0.07f;
        c.transform.SetParent(root.transform, false); c.transform.localPosition = new Vector3(0f, -0.015f, 0f);
        c.transform.localRotation = Quaternion.Euler(0f, 0f, 45f); c.transform.localScale = new Vector3(0.065f, 0.065f, 0.05f);
        foreach (var go in new[] { a, b, c }) KittyLook.MakeGlow(go, pink);
        root.AddComponent<FaceCamera>();
        root.AddComponent<HeartPop>();
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * 0.4f * Time.deltaTime;
        float s = age < 0.15f ? age / 0.15f : Mathf.Max(0f, 1f - (age - 0.6f) / 0.4f);
        transform.localScale = Vector3.one * Mathf.Min(1f, s);
        if (age > 1f) Destroy(gameObject);
    }
}
