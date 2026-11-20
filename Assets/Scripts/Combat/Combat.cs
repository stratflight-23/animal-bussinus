using System.Collections.Generic;
using UnityEngine;

// Shared "what happens when a weapon hits something" logic, used by every weapon.
public static class Combat
{
    static readonly HashSet<GameObject> seen = new HashSet<GameObject>();
    static readonly Collider[] buffer = new Collider[64];

    // The thing that should take the hit: whatever has Health, or a loose physics object.
    public static GameObject TargetOf(Collider col)
    {
        if (col == null) return null;
        var h = col.GetComponentInParent<Health>();
        if (h != null) return h.gameObject;
        var body = col.attachedRigidbody;
        if (body != null && !body.isKinematic) return body.gameObject;
        return null;
    }

    // Hit one target: damage + push + the weapon's effect.
    public static void HitTarget(WeaponData d, GameObject target, Vector3 point, Vector3 direction, GameObject attacker, float scale = 1f)
    {
        if (target == null || target == attacker) return;
        // Players in the spawn safe zone are untouchable: no damage, no knockback, no freeze/stun/teleport.
        if (SpawnSafeZone.Protects(target.GetComponent<Health>())) return;
        direction.y = Mathf.Max(direction.y, 0f);
        Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;

        var health = target.GetComponent<Health>();
        if (health != null && d.damage > 0f && d.effect != WeaponEffect.Heal)
            health.TakeDamage(d.damage * scale, point, dir);

        Vector3 push = dir * d.knockback * scale;
        if (d.knockback > 0f) push += Vector3.up * d.knockback * 0.3f * scale;
        EffectSystem.Push(target, push);

        EffectSystem.ApplyToTarget(d.effect, target, point, dir, d);
    }

    // Hit everything in a circle (explosions, area attacks, traps). Returns how many things got hit.
    public static int HitArea(WeaponData d, Vector3 center, float radius, GameObject skipA, GameObject skipB, float scale = 1f)
    {
        int n = Physics.OverlapSphereNonAlloc(center, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
        seen.Clear();
        int hits = 0;
        for (int i = 0; i < n; i++)
        {
            var target = TargetOf(buffer[i]);
            if (target == null || target == skipA || target == skipB || !seen.Add(target)) continue;

            Vector3 dir = target.transform.position - center; // negative knockback (magnets) pulls along this
            float dist = Vector3.Distance(center, target.transform.position);
            float falloff = Mathf.Lerp(1f, 0.5f, dist / Mathf.Max(radius, 0.01f));
            HitTarget(d, target, buffer[i].ClosestPointOnBounds(center), dir, null, scale * falloff);
            hits++;
        }
        return hits;
    }

    // A projectile or thrown item landed somewhere.
    public static void Impact(WeaponData d, Collider hitCollider, Vector3 point, Vector3 direction, GameObject attacker, GameObject self)
    {
        if (d.areaRadius > 0f)
        {
            FxSpawner.Explosion(point, d.areaRadius, d.accentColor);
            SoundFx.Play(Sfx.Boom, point);
            HitArea(d, point, d.areaRadius, attacker, self);
        }
        else
        {
            var target = TargetOf(hitCollider);
            if (target != null && target != attacker && target != self)
            {
                HitTarget(d, target, point, direction, attacker);
                SoundFx.Play(d.effect == WeaponEffect.Silly ? Sfx.Squeak : Sfx.Bonk, point, 0.6f);
            }
            else
            {
                FxSpawner.Burst(point, d.accentColor, 5, 2f, 0.05f);
            }
        }
        EffectSystem.PlaceEffect(d.effect, point);
    }
}
