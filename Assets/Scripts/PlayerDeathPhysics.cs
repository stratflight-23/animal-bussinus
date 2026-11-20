using UnityEngine;
using System.Collections;

// Put this on the same object as gorillalocomotion (the VR player / XR Origin).
// Call TriggerDeath(direction) from whatever kills the player, e.g. a lava trigger or enemy hit.
public class PlayerDeathPhysics : MonoBehaviour
{
    [Header("References (auto-filled if left empty)")]
    public Rigidbody playerRb;
    public gorillalocomotion movementScript;

    [Header("Death Settings")]
    public float launchForce = 15f;
    [Tooltip("Spin the player when they die. Spinning the camera in VR makes most people feel sick, so leave this off unless you're testing on a flat screen.")]
    public bool tumble = false;
    public float tumbleForce = 15f;

    [Header("Respawn Settings")]
    [Tooltip("Where to respawn. If empty, uses where the player started.")]
    public Transform respawnPoint;
    [Tooltip("Seconds after dying before you respawn. Set to 0 to turn off auto-respawn.")]
    public float respawnDelay = 3f;

    public bool IsDead { get; private set; }

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake()
    {
        if (playerRb == null) playerRb = GetComponent<Rigidbody>();
        if (movementScript == null) movementScript = GetComponent<gorillalocomotion>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void TriggerDeath(Vector3 hitDirection)
    {
        if (IsDead) return; // can't die twice at once
        // Backup check: nobody dies inside the spawn safe zone, even if something calls this directly.
        // (In multiplayer the server must enforce the safe zone.)
        if (SpawnSafeZone.IsInside(transform.position)) return;
        IsDead = true;

        // 1. Disable normal movement
        if (movementScript != null)
            movementScript.enabled = false;

        playerRb.useGravity = true; // in case they died while hanging on a wall

        // 2. Knockback (normalized so the force is the same no matter how hitDirection was calculated)
        Vector3 dir = hitDirection.sqrMagnitude > 0.001f ? hitDirection.normalized : -transform.forward;
        playerRb.linearVelocity = Vector3.zero;
        playerRb.AddForce((dir + Vector3.up * 0.5f).normalized * launchForce, ForceMode.Impulse);

        // 3. Optional tumble
        if (tumble)
        {
            playerRb.constraints = RigidbodyConstraints.None;
            playerRb.AddTorque(Random.insideUnitSphere * tumbleForce, ForceMode.Impulse);
        }

        if (respawnDelay > 0f)
            StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    public void Respawn()
    {
        // Stop all motion and put the player back upright
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.constraints = RigidbodyConstraints.FreezeRotation; // stand back up, no tipping over

        Vector3 pos = respawnPoint != null ? respawnPoint.position : startPosition;
        Quaternion rot = respawnPoint != null ? Quaternion.Euler(0f, respawnPoint.eulerAngles.y, 0f) : startRotation;
        playerRb.position = pos;
        playerRb.rotation = rot;
        transform.SetPositionAndRotation(pos, rot);

        // Turn movement back on
        if (movementScript != null)
            movementScript.enabled = true;

        IsDead = false;
    }
}
