using UnityEngine;

// Keeps text (labels, damage numbers) turned toward the player's head so it's always readable.
public class FaceCamera : MonoBehaviour
{
    [Tooltip("Only turn left/right (stays upright). Turn off to also tilt up/down.")]
    public bool yawOnly = true;

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 dir = transform.position - cam.transform.position;
        if (yawOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }
}
