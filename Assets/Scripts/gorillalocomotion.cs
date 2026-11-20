using UnityEngine;
using UnityEngine.XR;

// Put this on the root of your XR rig (the object that has the Rigidbody + CapsuleCollider).
// File name must be exactly: gorillalocomotion.cs
[RequireComponent(typeof(Rigidbody))]
public class gorillalocomotion : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The VR camera (your head). Movement goes the way you're looking. Auto-finds Main Camera if empty.")]
    public Transform head;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 6f;
    [Tooltip("How quickly you speed up / change direction. Higher = snappier.")]
    public float acceleration = 40f;
    [Tooltip("How much steering you get in the air (0 = none, 1 = same as ground).")]
    [Range(0f, 1f)] public float airControl = 0.3f;
    [Tooltip("Ignore tiny thumbstick drift.")]
    public float stickDeadzone = 0.15f;

    [Header("Turning")]
    [Tooltip("Flick the right stick left/right to turn this many degrees (0 = off, turn your body instead).")]
    public float snapTurnDegrees = 45f;

    [Header("Wall Grab Settings")]
    [Tooltip("Max seconds you can hang on walls before touching the ground again.")]
    public float maxClimbTime = 10f;
    public float wallJumpMultiplier = 1.5f;
    [Tooltip("Short delay after letting go so you don't instantly re-stick to the same wall.")]
    public float regrabCooldown = 0.3f;

    // Set by effects (sticky = slower, frozen = 0, speed juice = faster). 1 = normal.
    [HideInInspector] public float speedMultiplier = 1f;

    // --- state ---
    private Rigidbody rb;
    private float climbTimeLeft;
    private bool isClinging;
    private Vector3 wallNormal;
    private float regrabTimer;

    // contact info, refreshed every physics step
    private bool isGrounded;
    private bool touchingWall;
    private Vector3 touchedWallNormal;

    // input
    private Vector2 moveInput;
    private bool gripHeld;
    private bool jumpHeldLastFrame;
    private bool jumpQueued;
    private float turnQueued;
    private bool turnStickWasCentered = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;          // smooth in VR, no jitter
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // don't fly through walls

        if (head == null && Camera.main != null)
            head = Camera.main.transform;

        climbTimeLeft = maxClimbTime;
    }

    // Read controller input every frame (Update), do physics in FixedUpdate.
    void Update()
    {
        // Left thumbstick = walk
        moveInput = Vector2.zero;
        if (VRInput.Stick(XRNode.LeftHand, out Vector2 stick) && stick.magnitude > stickDeadzone)
            moveInput = Vector2.ClampMagnitude(stick, 1f);

        // Grip on either hand = grab wall
        bool leftGrip = VRInput.Grip(XRNode.LeftHand);
        bool rightGrip = VRInput.Grip(XRNode.RightHand);
        // A hand that's holding or touching an item (or a handle like the copter lever) uses its grip
        // for that, not for walls.
        if (HandGrabber.IsBusy(XRNode.LeftHand)) leftGrip = false;
        if (HandGrabber.IsBusy(XRNode.RightHand)) rightGrip = false;
        gripHeld = leftGrip || rightGrip;

        // Right A button = jump (only on the press, not while held)
        // Right stick flick = snap turn (once per flick; push it back to the middle to turn again)
        float turnX = VRInput.Stick(XRNode.RightHand).x;
        if (snapTurnDegrees > 0f && turnStickWasCentered && Mathf.Abs(turnX) > 0.75f)
        {
            turnQueued += Mathf.Sign(turnX) * snapTurnDegrees;
            turnStickWasCentered = false;
        }
        if (Mathf.Abs(turnX) < 0.3f) turnStickWasCentered = true;

        bool jumpHeld = VRInput.Primary(XRNode.RightHand);
        if (jumpHeld && !jumpHeldLastFrame)
            jumpQueued = true;
        jumpHeldLastFrame = jumpHeld;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (regrabTimer > 0f) regrabTimer -= dt;
        if (turnQueued != 0f) { SnapTurn(turnQueued); turnQueued = 0f; }

        // Climb timer only refills when you touch the ground
        if (isGrounded) climbTimeLeft = maxClimbTime;

        if (isClinging)
        {
            climbTimeLeft -= dt;
            rb.linearVelocity = Vector3.zero; // hold still on the wall

            if (jumpQueued || !gripHeld)
                ReleaseWall(true);   // let go or press jump = fling off the wall
            else if (climbTimeLeft <= 0f)
                ReleaseWall(false);  // out of stamina = just slide off
        }
        else
        {
            bool canGrab = touchingWall && gripHeld && !isGrounded
                           && regrabTimer <= 0f && climbTimeLeft > 0f;

            if (canGrab)
                StartCling(touchedWallNormal);
            else
                Move(dt);
        }

        // Clear per-step flags. OnCollisionStay runs after this and sets them again
        // for the next step, so they're always accurate (no stuck "grounded" state).
        jumpQueued = false;
        isGrounded = false;
        touchingWall = false;
    }

    void Move(float dt)
    {
        // Direction you're looking, flattened so looking up/down doesn't change speed
        Vector3 forward = head != null ? head.forward : transform.forward;
        Vector3 right = head != null ? head.right : transform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
        Vector3 target = (forward * moveInput.y + right * moveInput.x) * moveSpeed * speedMultiplier;

        // In the air with no stick input, keep your momentum (so wall-jumps carry you)
        bool steering = isGrounded || moveInput.sqrMagnitude > 0f;
        if (steering)
        {
            float control = isGrounded ? 1f : airControl;
            horizontal = Vector3.MoveTowards(horizontal, target, acceleration * control * dt);
        }

        float vertical = vel.y;
        if (jumpQueued && isGrounded && speedMultiplier > 0.01f) // can't jump while frozen/stunned
            vertical = jumpForce;

        rb.linearVelocity = new Vector3(horizontal.x, vertical, horizontal.z);
    }

    // Turned off (dying, flying, teleporting): forget any wall you were hanging on.
    // Otherwise you'd get flung off a wall the moment you respawn.
    void OnDisable()
    {
        if (isClinging)
        {
            isClinging = false;
            if (rb != null) rb.useGravity = true;
        }
        jumpQueued = false;
        turnQueued = 0f;
    }

    // Turn the whole player around the HEAD (not the feet), so the world doesn't swing past you.
    void SnapTurn(float degrees)
    {
        Quaternion q = Quaternion.Euler(0f, degrees, 0f);
        Vector3 pivot = head != null ? head.position : rb.position;
        Vector3 newPos = pivot + q * (rb.position - pivot);
        newPos.y = rb.position.y;
        Quaternion newRot = q * rb.rotation;
        rb.position = newPos;
        rb.rotation = newRot;
        transform.SetPositionAndRotation(newPos, newRot);
        rb.linearVelocity = q * rb.linearVelocity;   // keep moving the way you were going, turned too
    }

    void StartCling(Vector3 normal)
    {
        isClinging = true;
        wallNormal = normal;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        // NOTE: timer is NOT reset here. It only refills on the ground,
        // so you can't regrab forever.
    }

    void ReleaseWall(bool jumpOff)
    {
        isClinging = false;
        rb.useGravity = true;
        regrabTimer = regrabCooldown;

        if (jumpOff)
        {
            Vector3 jumpDirection = (wallNormal + Vector3.up * 1.2f).normalized;
            rb.AddForce(jumpDirection * (jumpForce * wallJumpMultiplier), ForceMode.Impulse);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;

            if (n.y > 0.5f)
            {
                isGrounded = true;           // floor
            }
            else if (Mathf.Abs(n.y) < 0.3f)
            {
                touchingWall = true;         // vertical wall
                touchedWallNormal = n;
            }
        }
    }
}
