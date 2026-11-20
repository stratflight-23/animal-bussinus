using System;
using System.Collections;
using UnityEngine;

// Put this on anything a VR hand can pick up. It needs a Rigidbody and at least one collider.
// While held it rides on the hand; when let go it's thrown with the hand's speed.
[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    [Tooltip("Where the item sits in your hand (moves the item relative to the hand).")]
    public Vector3 gripPositionOffset;
    [Tooltip("How the item is turned in your hand (degrees).")]
    public Vector3 gripRotationOffset;

    public HandGrabber Holder { get; private set; }
    public bool IsHeld => Holder != null;
    public bool HasBeenGrabbed { get; private set; }
    public GameObject LastHolderPlayer { get; private set; }

    public event Action<HandGrabber> Grabbed;
    public event Action<HandGrabber, Vector3> Released;      // hand, throw velocity
    public event Action<HandGrabber> TriggerPressed;         // trigger just pressed
    public event Action<HandGrabber> TriggerHeld;            // trigger still held (every frame)

    Rigidbody rb;
    Collider[] colliders;

    public Rigidbody Body => rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
    }

    // ---- called by HandGrabber

    public void OnGrab(HandGrabber hand)
    {
        Holder = hand;
        HasBeenGrabbed = true;
        LastHolderPlayer = hand.PlayerRoot;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        SetColliders(false); // held items don't bump into you or set off lava
        Grabbed?.Invoke(hand);
    }

    public void OnRelease(HandGrabber hand, Vector3 velocity, Vector3 angularVelocity)
    {
        Holder = null;
        transform.SetParent(null, true);
        SetColliders(true);
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = velocity;
        rb.angularVelocity = angularVelocity;
        if (hand.PlayerRoot != null && isActiveAndEnabled)
            StartCoroutine(IgnorePlayerBriefly(hand.PlayerRoot));
        Released?.Invoke(hand, velocity);
    }

    public void FireTriggerPressed(HandGrabber hand) => TriggerPressed?.Invoke(hand);
    public void FireTriggerHeld(HandGrabber hand) => TriggerHeld?.Invoke(hand);

    // Drop it from the hand (no throw). Used by traps and single-use items.
    public void ForceRelease()
    {
        if (Holder != null) Holder.Release(false);
    }

    void OnDestroy()
    {
        if (Holder != null) Holder.ForgetItem(this);
    }

    void SetColliders(bool on)
    {
        foreach (var c in colliders)
            if (c != null) c.enabled = on;
    }

    // So a thrown item doesn't instantly hit your own body.
    IEnumerator IgnorePlayerBriefly(GameObject player)
    {
        var playerCols = player.GetComponentsInChildren<Collider>();
        SetIgnore(playerCols, true);
        yield return new WaitForSeconds(0.3f);
        SetIgnore(playerCols, false);
    }

    void SetIgnore(Collider[] playerCols, bool ignore)
    {
        foreach (var mine in colliders)
            foreach (var theirs in playerCols)
                if (mine != null && theirs != null && mine.enabled && theirs.enabled)
                    Physics.IgnoreCollision(mine, theirs, ignore);
    }
}
