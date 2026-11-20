using System.Collections.Generic;
using UnityEngine;

// The ONE weapon script. It reads a WeaponData "recipe card" and does the right kind of attack:
//   Swing / Stab : hits when you move it fast (stab = thrust forward)
//   Throw        : let go while moving your hand to throw; it does its effect when it lands
//   Shoot        : trigger fires shots from the tip (hold trigger to keep firing)
//   Trap         : trigger drops it; after a second it's armed and goes off when someone comes close
//   Use          : trigger uses it on yourself (or everyone around you for area items)
[RequireComponent(typeof(Grabbable))]
public class Weapon : MonoBehaviour
{
    public WeaponData data;

    const float SwingSpeedNeeded = 2.5f; // meters/second at the tip
    const float StabSpeedNeeded = 1.5f;

    Grabbable grab;
    Rigidbody rb;
    Light glowLight;
    float nextUse;

    // melee
    Vector3 lastTip, tipVelocity;
    bool hasLastTip;
    readonly Dictionary<GameObject, float> lastHit = new Dictionary<GameObject, float>();
    readonly Collider[] overlap = new Collider[32];

    // thrown
    bool thrownArmed;
    float thrownAt;
    GameObject thrower;
    bool returning;

    // trap
    bool trapArmed;
    float trapReadyAt, ownerSafeUntil;
    GameObject trapOwner;

    void Awake()
    {
        grab = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        glowLight = GetComponentInChildren<Light>(true);
        grab.Grabbed += OnGrabbed;
        grab.Released += OnReleased;
        grab.TriggerPressed += OnTriggerPressed;
        grab.TriggerHeld += OnTriggerHeld;
    }

    Vector3 TipWorld => transform.TransformPoint(data.tipOffset);
    GameObject Owner => grab.Holder != null ? grab.Holder.PlayerRoot : null;

    // ------------------------------------------------------------ grab / release

    void OnGrabbed(HandGrabber hand)
    {
        hasLastTip = false;
        thrownArmed = false;
        returning = false;
        trapArmed = false;
        rb.useGravity = true;
    }

    void OnReleased(HandGrabber hand, Vector3 velocity)
    {
        if (data == null) return;
        if (data.attackType == AttackType.Throw && velocity.magnitude > 1.5f)
        {
            thrownArmed = true;
            thrownAt = Time.time;
            thrower = hand.PlayerRoot;
            SoundFx.Play(Sfx.Whoosh, transform.position, 0.5f);
        }
    }

    // ------------------------------------------------------------ trigger

    void OnTriggerPressed(HandGrabber hand)
    {
        if (data == null) return;
        switch (data.attackType)
        {
            case AttackType.Shoot: TryShoot(hand); break;
            case AttackType.Trap: PlaceTrap(hand); break;
            case AttackType.Use: TryUse(hand); break;
            default:
                if (glowLight != null) glowLight.enabled = !glowLight.enabled; // torches etc.
                break;
        }
    }

    void OnTriggerHeld(HandGrabber hand)
    {
        if (data != null && data.attackType == AttackType.Shoot) TryShoot(hand);
    }

    void TryShoot(HandGrabber hand)
    {
        if (Time.time < nextUse) return;
        nextUse = Time.time + data.cooldown;
        Vector3 fwd = transform.forward;
        Projectile.Fire(data, TipWorld + fwd * 0.08f, fwd * data.projectileSpeed, hand.PlayerRoot);
        SoundFx.Play(data.effect == WeaponEffect.Silly ? Sfx.Squeak : Sfx.Pew, TipWorld, 0.5f);
        hand.Buzz(0.35f, 0.05f);
    }

    void TryUse(HandGrabber hand)
    {
        if (Time.time < nextUse) return;
        nextUse = Time.time + data.cooldown;
        GameObject player = hand.PlayerRoot;

        if (data.spawnsBlock)
        {
            EffectSystem.SpawnGoopWall(player, hand.transform);
        }
        else if (data.effect == WeaponEffect.None && glowLight != null)
        {
            glowLight.enabled = !glowLight.enabled;
            SoundFx.Play(Sfx.Chime, transform.position, 0.3f);
        }
        else if (data.affectsArea)
        {
            Vector3 center = player.transform.position + Vector3.up;
            FxSpawner.Ring(player.transform.position + Vector3.up * 0.05f, data.range, data.accentColor);
            Combat.HitArea(data, center, data.range, player, gameObject);
            EffectSystem.PlaceEffect(data.effect, center);
            SoundFx.Play(data.knockback >= 8f ? Sfx.Boom : Sfx.Whoosh, center, 0.6f);
        }
        else
        {
            EffectSystem.ApplyToSelf(data, player, hand.transform);
        }

        hand.Buzz(0.5f, 0.1f);
        if (data.singleUse)
        {
            grab.ForceRelease();
            Destroy(gameObject);
        }
    }

    void PlaceTrap(HandGrabber hand)
    {
        trapOwner = hand.PlayerRoot;
        grab.ForceRelease();             // drops straight down
        rb.linearVelocity = Vector3.down;
        trapArmed = true;
        trapReadyAt = Time.time + 1f;
        ownerSafeUntil = Time.time + 3f; // you can walk away before it can catch you
        SoundFx.Play(Sfx.Bonk, transform.position, 0.4f);
    }

    // ------------------------------------------------------------ every frame

    void Update()
    {
        if (data == null) return;
        if (grab.IsHeld) MeleeCheck();
        else hasLastTip = false;

        if (trapArmed) TrapCheck();
        if (thrownArmed && Time.time - thrownAt > 6f) EndThrow();
    }

    void FixedUpdate()
    {
        if (!thrownArmed || !data.returnsAfterThrow || thrower == null) return;
        float t = Time.time - thrownAt;
        if (t < 0.6f) return;

        // Boomerang: curve back to the thrower's chest
        returning = true;
        rb.useGravity = false;
        Vector3 target = thrower.transform.position + Vector3.up * 1.2f;
        if (Camera.main != null) target = Camera.main.transform.position - Vector3.up * 0.4f;
        Vector3 to = target - rb.position;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, to.normalized * 9f, 4f * Time.fixedDeltaTime);
        rb.angularVelocity = transform.up * 20f;
        if (to.magnitude < 0.8f || t > 4f) EndThrow();
    }

    void MeleeCheck()
    {
        if (data.attackType != AttackType.Swing && data.attackType != AttackType.Stab) return;

        Vector3 tip = TipWorld;
        if (!hasLastTip)
        {
            lastTip = tip;
            hasLastTip = true;
            tipVelocity = Vector3.zero;
            return;
        }
        Vector3 v = (tip - lastTip) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastTip = tip;
        tipVelocity = Vector3.Lerp(tipVelocity, v, 0.5f);

        float speed = data.attackType == AttackType.Stab ? Vector3.Dot(tipVelocity, transform.forward) : tipVelocity.magnitude;
        float needed = data.attackType == AttackType.Stab ? StabSpeedNeeded : SwingSpeedNeeded;
        if (speed < needed) return;

        GameObject owner = Owner;
        float scale = Mathf.Clamp(speed / 4f, 0.6f, 1.5f);

        // check a few spots along the weapon, from the middle to the tip
        for (int s = 0; s < 3; s++)
        {
            Vector3 p = transform.TransformPoint(data.tipOffset * (0.45f + 0.275f * s));
            int n = Physics.OverlapSphereNonAlloc(p, data.range, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = overlap[i];
                if (col.transform.IsChildOf(transform)) continue;
                var target = Combat.TargetOf(col);
                if (target == null || target == owner) continue;
                if (lastHit.TryGetValue(target, out float when) && Time.time - when < Mathf.Max(data.cooldown, 0.2f)) continue;
                lastHit[target] = Time.time;

                Vector3 point = col.ClosestPointOnBounds(p);
                Combat.HitTarget(data, target, point, tipVelocity, owner, scale);
                EffectSystem.PlaceEffect(data.effect, point);
                SoundFx.Play(data.effect == WeaponEffect.Silly ? Sfx.Squeak : Sfx.Bonk, point, 0.7f);
                if (grab.Holder != null) grab.Holder.Buzz(0.7f, 0.08f);
            }
        }
    }

    void TrapCheck()
    {
        if (grab.IsHeld) { trapArmed = false; return; }
        if (Time.time < trapReadyAt) return;

        int n = Physics.OverlapSphereNonAlloc(transform.position, data.range, overlap, ~0, QueryTriggerInteraction.Ignore);
        GameObject skipOwner = Time.time < ownerSafeUntil ? trapOwner : null;
        bool triggered = false;
        for (int i = 0; i < n && !triggered; i++)
        {
            var target = Combat.TargetOf(overlap[i]);
            if (target == null || target == gameObject || target == skipOwner) continue;
            if (target.GetComponent<Health>() == null) continue; // only living things set traps off
            triggered = true;
        }
        if (!triggered) return;

        trapArmed = false;
        Vector3 center = transform.position + Vector3.up * 0.3f;
        FxSpawner.Ring(transform.position, data.range, data.accentColor);
        Combat.HitArea(data, center, Mathf.Max(data.range, data.areaRadius), skipOwner, gameObject);
        EffectSystem.PlaceEffect(data.effect, center);
        SoundFx.Play(Sfx.Boom, center, 0.5f);
        if (data.singleUse) Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!thrownArmed || data == null) return;
        var target = Combat.TargetOf(collision.collider);
        if (target != null && target == thrower) return;

        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Vector3 dir = rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity : -collision.relativeVelocity;
        Combat.Impact(data, collision.collider, point, dir, thrower, gameObject);

        if (data.singleUse) Destroy(gameObject);
        else EndThrow();
    }

    void EndThrow()
    {
        thrownArmed = false;
        if (returning) rb.useGravity = true;
        returning = false;
    }
}
