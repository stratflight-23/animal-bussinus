using UnityEngine;

// Put this on any object that should kill the player (lava, spikes, enemies).
// The object's collider must be set to "Is Trigger".
// It kills through the player's Health (Health.Kill), so the spawn safe zone protects you here too.
// (OnTriggerStay: if you're standing in lava inside the safe zone and step out of the zone, it gets you.)
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other) => Hurt(other);
    void OnTriggerStay(Collider other) => Hurt(other);

    void Hurt(Collider other)
    {
        PlayerDeathPhysics death = other.GetComponentInParent<PlayerDeathPhysics>();
        if (death == null || death.IsDead) return;

        // Knock the player away from the center of the kill zone
        Vector3 away = other.transform.position - transform.position;
        away.y = 0f;

        var health = death.GetComponent<Health>();
        if (health != null) health.Kill(away);   // normal way: through Health (respects the safe zone)
        else death.TriggerDeath(away);           // older scenes without Health
    }
}
