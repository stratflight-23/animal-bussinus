using System;
using UnityEngine;

// Keeps track of effects on one player or dummy: sticky (slow), frozen, burning, stunned, speed boost.
// Added automatically to GorillaPlayer and training dummies.
public class StatusEffects : MonoBehaviour
{
    float slowUntil, frozenUntil, stunnedUntil, burnUntil, speedUntil;
    float burnDps, nextBurnTick;

    public bool IsSlowed => Time.time < slowUntil;
    public bool IsFrozen => Time.time < frozenUntil;
    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsBurning => Time.time < burnUntil;
    public bool IsFast => Time.time < speedUntil;

    public event Action<WeaponEffect> EffectApplied;

    // How fast this player may move right now (1 = normal).
    public float MoveMultiplier
    {
        get
        {
            if (IsFrozen || IsStunned) return 0f;
            float m = 1f;
            if (IsSlowed) m *= 0.4f;
            if (IsFast) m *= 1.8f;
            return m;
        }
    }

    Health health;
    gorillalocomotion loco;
    Rigidbody rb;
    PrimitiveColor[] parts;
    bool wasTinted;

    void Awake()
    {
        health = GetComponent<Health>();
        loco = GetComponent<gorillalocomotion>();
        rb = GetComponent<Rigidbody>();
        parts = GetComponentsInChildren<PrimitiveColor>(true);
    }

    public void Slow(float seconds) { slowUntil = Mathf.Max(slowUntil, Time.time + seconds); Announce(WeaponEffect.Sticky, "STICKY!", FxSpawner.GoopGreen); }
    public void SpeedBoost(float seconds) { speedUntil = Mathf.Max(speedUntil, Time.time + seconds); Announce(WeaponEffect.Speed, "ZOOM!", Color.cyan); }

    public void Freeze(float seconds)
    {
        frozenUntil = Mathf.Max(frozenUntil, Time.time + seconds);
        if (rb != null && !rb.isKinematic) rb.linearVelocity = new Vector3(0f, Mathf.Min(0f, rb.linearVelocity.y), 0f);
        Announce(WeaponEffect.Freeze, "FROZEN!", FxSpawner.Ice);
    }

    public void Stun(float seconds)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
        Announce(WeaponEffect.Shock, "ZAPPED!", FxSpawner.Spark);
    }

    public void Burn(float damagePerSecond, float seconds)
    {
        burnDps = IsBurning ? Mathf.Max(burnDps, damagePerSecond) : damagePerSecond;
        burnUntil = Mathf.Max(burnUntil, Time.time + seconds);
        nextBurnTick = Time.time + 0.5f;
        Announce(WeaponEffect.Fire, "ON FIRE!", FxSpawner.FireOrange);
    }

    // Shows a word over dummies (not over the player's own head, that would be in your face in VR).
    public void Announce(WeaponEffect effect, string word, Color color)
    {
        if (loco == null && word != null)
            FloatingText.Spawn(word, transform.position + Vector3.up * 2.4f, color, 1f);
        EffectApplied?.Invoke(effect);
    }

    void Update()
    {
        if (loco != null) loco.speedMultiplier = MoveMultiplier;

        if (IsBurning && health != null && Time.time >= nextBurnTick)
        {
            nextBurnTick = Time.time + 0.5f;
            health.TakeDamage(burnDps * 0.5f, transform.position + Vector3.up, Vector3.zero);
        }

        // color tint on dummies
        if (parts.Length == 0) return;
        bool flicker = Mathf.Sin(Time.time * 30f) > 0f;
        Color? tint = IsFrozen ? FxSpawner.Ice
                    : IsStunned ? (flicker ? FxSpawner.Spark : (Color?)null)
                    : IsBurning ? (flicker ? FxSpawner.FireOrange : new Color(1f, 0.25f, 0.1f))
                    : IsSlowed ? FxSpawner.GoopGreen
                    : IsFast ? Color.cyan
                    : (Color?)null;

        if (tint.HasValue)
        {
            foreach (var p in parts) if (p != null) p.SetTint(tint.Value);
            wasTinted = true;
        }
        else if (wasTinted)
        {
            foreach (var p in parts) if (p != null) p.ClearTint();
            wasTinted = false;
        }
    }
}
