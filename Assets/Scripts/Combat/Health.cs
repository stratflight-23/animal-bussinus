using System;
using UnityEngine;

// Hit points for anything that can be hurt: the player, training dummies, later other players.
// If it's on the player (next to PlayerDeathPhysics), dying uses the normal death + respawn.
//
// SAFE ZONE: every kind of damage (weapons, kittens, burning, lava...) goes through TakeDamage or Kill below,
// and both refuse to hurt a player standing in the spawn safe zone (SpawnSafeZone). One check, one place.
public class Health : MonoBehaviour
{
    [Tooltip("Most health this can have. Players start with 100. Health refills to this on respawn.")]
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsShielded => Time.time < shieldUntil;
    // True while this is a player standing inside the spawn safe zone (can't be hurt).
    public bool IsInSafeZone => SpawnSafeZone.Protects(this);

    public event Action<float, Vector3> Damaged;   // amount, where it hit
    public event Action<float> Healed;
    public event Action Blocked;                   // a shield stopped the hit
    public event Action Died;
    public event Action Revived;

    float shieldUntil;
    PlayerDeathPhysics playerDeath;

    void Awake()
    {
        CurrentHealth = maxHealth;
        playerDeath = GetComponent<PlayerDeathPhysics>();
    }

    void Update()
    {
        // The player's death script respawns them on its own; refill health when it does.
        if (IsDead && playerDeath != null && !playerDeath.IsDead)
            Revive();
    }

    // Returns true if the hit did damage.
    public bool TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (IsDead || amount <= 0f) return false;

        // Players in the spawn safe zone can't take ANY damage.
        // MULTIPLAYER NOTE: in multiplayer the SERVER must make this check (it decides who is in the safe
        // zone and blocks the damage), so a hacked game can't get around it.
        if (IsInSafeZone) return false;

        if (IsShielded)
        {
            Blocked?.Invoke();
            return false;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Damaged?.Invoke(amount, hitPoint);
        if (CurrentHealth <= 0f) Die(hitDirection);
        return true;
    }

    // Instantly kills (used by lava / kill zones). Goes through the same safe zone check as all other damage.
    // Shields don't stop it.
    public void Kill(Vector3 hitDirection)
    {
        if (IsDead || IsInSafeZone) return;
        float amount = CurrentHealth;
        CurrentHealth = 0f;
        Damaged?.Invoke(amount, transform.position);
        Die(hitDirection);
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        Healed?.Invoke(amount);
    }

    public void Shield(float seconds)
    {
        shieldUntil = Mathf.Max(shieldUntil, Time.time + seconds);
    }

    void Die(Vector3 hitDirection)
    {
        IsDead = true;
        Died?.Invoke();
        if (playerDeath != null) playerDeath.TriggerDeath(hitDirection);
    }

    public void Revive()
    {
        IsDead = false;
        CurrentHealth = maxHealth;
        shieldUntil = 0f;
        Revived?.Invoke();
    }
}
