using UnityEngine;

// A glowing doorway. Walk into it and you're moved to "destination" (and turned to face the way it faces).
// Anything you're holding comes with you, so you can carry loot home.
// The portal INTO the temple has "Respawn Temple Loot" ticked: every trip in, the temple gets fresh loot.
[RequireComponent(typeof(Collider))]
public class TemplePortal : MonoBehaviour
{
    [Tooltip("Where you come out. Put it a few meters in front of the other portal, facing away from it.")]
    public Transform destination;
    [Tooltip("Re-roll all the temple loot when someone goes through (tick this on the portal that goes INTO the temple).")]
    public bool respawnTempleLoot;
    [Tooltip("The temple's area. Loot still lying inside this box gets replaced; loot carried out of it is kept.")]
    public Bounds templeArea = new Bounds(new Vector3(0f, 10f, 400f), new Vector3(160f, 80f, 160f));

    static float nextAllowed; // stops you bouncing straight back through the other portal

    // Reset when Play starts. (This project uses fast Play mode, which keeps "static" values between runs;
    // without this, the portals could stay switched off for minutes after pressing Play again.)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => nextAllowed = 0f;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerDeathPhysics>();
        if (player == null || player.IsDead || destination == null || Time.time < nextAllowed) return;
        nextAllowed = Time.time + 1f;

        Vector3 pos = destination.position;
        Quaternion rot = Quaternion.Euler(0f, destination.eulerAngles.y, 0f);
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
        }
        player.transform.SetPositionAndRotation(pos, rot);
        Physics.SyncTransforms();

        // if you were hanging on a wall, let go (turning movement off and on resets it)
        var loco = player.GetComponent<gorillalocomotion>();
        if (loco != null && loco.enabled)
        {
            loco.enabled = false;
            loco.enabled = true;
        }

        if (respawnTempleLoot) LootSpawnPoint.RespawnAll(templeArea);

        FxSpawner.Burst(pos + Vector3.up, FxSpawner.GoopGreen, 12, 2f, 0.06f);
        SoundFx.Play(Sfx.Whoosh, pos, 0.8f);
    }
}
