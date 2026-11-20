using UnityEngine;

// Put this on any object that should kill the player (lava, spikes, enemies).
// The object's collider must be set to "Is Trigger".
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerDeathPhysics death = other.GetComponentInParent<PlayerDeathPhysics>();
        if (death == null) return;

        // Knock the player away from the center of the kill zone
        Vector3 away = other.transform.position - transform.position;
        away.y = 0f;
        death.TriggerDeath(away);
    }
}
