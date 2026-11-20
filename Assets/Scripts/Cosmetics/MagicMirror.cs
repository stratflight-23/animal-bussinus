using UnityEngine;

// A "mirror": a camera behind the glass films you and shows it (flipped, like a real mirror) on the glass.
// To save power on Quest, the camera only runs while someone is standing close to the mirror.
public class MagicMirror : MonoBehaviour
{
    public Camera mirrorCamera;
    [Tooltip("The mirror camera turns on when your head is closer than this (meters).")]
    public float activeRange = 5f;

    void Awake()
    {
        // A camera that draws into a picture must not try to draw for both VR eyes.
        if (mirrorCamera != null) mirrorCamera.stereoTargetEye = StereoTargetEyeMask.None;
    }

    void Update()
    {
        if (mirrorCamera == null) return;
        bool near = PokePanel.PlayerNear(transform.position, activeRange);
        if (mirrorCamera.enabled != near) mirrorCamera.enabled = near;
    }
}
