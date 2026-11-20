using System;
using System.Collections.Generic;
using UnityEngine;

// Our original low-poly gorilla (Assets/Characters/LowPolyGorilla.prefab). It sits under GorillaPlayer and every frame:
//   - the HEAD follows the headset,
//   - the hunched BODY hangs below/behind the head and only turns left/right,
//   - the C-shaped HANDS follow the controllers ("Left Hand" / "Right Hand"),
//   - each ARM bends at its elbow ring between the shoulder and the hand (and stretches if you reach far).
// Your own head + body are put on the "SelfHidden" layer so YOUR camera doesn't draw them (mirrors and other players
// still see them). Arms and hands stay visible to you.
// Attachment points for cosmetics / outfits / pets: Head_Top, Face, Neck, Back, Chest, LeftHand, RightHand.
public class GorillaModel : MonoBehaviour
{
    [Serializable]
    public class TintPart
    {
        public Renderer renderer;
        [Tooltip("1 = body color, lower = darker facet (0.76 = #5d8aa3, 0.62 = #4c7489 for the default blue).")]
        public float shade = 1f;
    }

    [Serializable]
    public class Arm
    {
        public Transform upperArm, elbow, forearm, hand;
        [HideInInspector] public Transform controller;
    }

    [Header("Moving parts")]
    public Transform head;
    public Transform body;
    public Arm leftArm = new Arm();
    public Arm rightArm = new Arm();

    [Header("Attachment points")]
    public Transform headTop;
    public Transform face, neck, back, chest, leftHandPoint, rightHandPoint;

    [Header("Look")]
    [Tooltip("Main body color (cosmetic Body Colors replace this).")]
    public Color bodyColor = new Color(0x7a / 255f, 0xa9 / 255f, 0xc2 / 255f);
    public List<TintPart> tintParts = new List<TintPart>();

    [Header("Fitting it to you")]
    [Tooltip("Head center, relative to the headset (so the eyes are about where your eyes are).")]
    public Vector3 headOffset = new Vector3(0f, 0.02f, -0.08f);
    [Tooltip("Body center, relative to the headset, turned the way you face. Negative z = body sits behind the head.")]
    public Vector3 bodyOffset = new Vector3(0f, -0.46f, -0.17f);
    [Tooltip("Shoulder position on the body (the left one uses -x).")]
    public Vector3 shoulderOffset = new Vector3(0.33f, 0.2f, 0.04f);
    [Tooltip("Where the forearm meets the hand, relative to the controller.")]
    public Vector3 wristOffset = new Vector3(0f, 0f, -0.1f);
    public float upperArmLength = 0.34f;
    public float forearmLength = 0.34f;
    public float upperArmThickness = 0.17f;
    public float forearmThickness = 0.19f;

    Transform cam;
    bool ready;

    void Start()
    {
        var root = transform.parent != null ? transform.parent : transform;
        var camera = root.GetComponentInChildren<Camera>();
        cam = camera != null ? camera.transform : null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Left Hand") leftArm.controller = t;
            if (t.name == "Right Hand") rightArm.controller = t;
        }

        // hide the old sphere hands (our C-shaped hands replace them); the sphere's trigger still presses buttons
        foreach (var c in new[] { leftArm.controller, rightArm.controller })
        {
            var r = c != null ? c.GetComponent<Renderer>() : null;
            if (r != null) r.enabled = false;
        }

        int hidden = LayerMask.NameToLayer(PlayerAvatar.SelfHiddenLayerName);
        if (hidden >= 0)
        {
            SetLayer(head, hidden);
            SetLayer(body, hidden);
            if (camera != null) camera.cullingMask &= ~(1 << hidden);
        }

        // (colors are already set on the prefab; PlayerAvatar re-tints it for Body Color cosmetics)
        ready = cam != null;
        Pose();
        Application.onBeforeRender += Pose;
    }

    void OnDestroy() => Application.onBeforeRender -= Pose;

    void LateUpdate() => Pose();

    // ------------------------------------------------------------ color

    public void SetTint(Color c)
    {
        foreach (var p in tintParts)
        {
            if (p == null || p.renderer == null) continue;
            var pc = p.renderer.GetComponent<PrimitiveColor>();
            if (pc == null) pc = p.renderer.gameObject.AddComponent<PrimitiveColor>();
            Color shaded = c * p.shade;
            shaded.a = 1f;
            pc.SetColor(shaded);
        }
    }

    // ------------------------------------------------------------ following the headset + controllers

    void Pose()
    {
        if (!ready || cam == null) return;

        Vector3 fwd = cam.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = cam.up;
        fwd.y = 0f;
        Quaternion yaw = Quaternion.LookRotation(fwd.sqrMagnitude > 0.001f ? fwd.normalized : transform.forward);

        if (head != null) head.SetPositionAndRotation(cam.position + cam.rotation * headOffset, cam.rotation);
        if (body != null) body.SetPositionAndRotation(cam.position + yaw * bodyOffset, yaw);

        PoseArm(leftArm, -1f);
        PoseArm(rightArm, 1f);
    }

    void PoseArm(Arm arm, float side)
    {
        if (arm.controller == null || body == null) return;

        // the hand sits exactly on the controller
        if (arm.hand != null) arm.hand.SetPositionAndRotation(arm.controller.position, arm.controller.rotation);

        Vector3 shoulder = body.TransformPoint(new Vector3(shoulderOffset.x * side, shoulderOffset.y, shoulderOffset.z));
        Vector3 wrist = arm.controller.position + arm.controller.rotation * wristOffset;
        Vector3 toWrist = wrist - shoulder;
        float d = Mathf.Max(0.01f, toWrist.magnitude);
        Vector3 dir = toWrist / d;
        float l1 = upperArmLength, l2 = forearmLength;

        Vector3 elbow;
        if (d >= l1 + l2 - 0.001f)
        {
            // reaching far: the arm goes straight and stretches
            elbow = shoulder + dir * (d * l1 / (l1 + l2));
        }
        else
        {
            // simple 2-bone bend: the elbow points down, out to the side and a little back (like a gorilla's)
            d = Mathf.Max(d, Mathf.Abs(l1 - l2) + 0.02f);
            float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            Vector3 pole = Vector3.down + body.right * side * 0.7f - body.forward * 0.3f;
            pole = Vector3.ProjectOnPlane(pole, dir);
            if (pole.sqrMagnitude < 0.0001f) pole = Vector3.ProjectOnPlane(Vector3.down, dir);
            elbow = shoulder + dir * a + pole.normalized * h;
        }

        Segment(arm.upperArm, shoulder, elbow, upperArmThickness);
        Segment(arm.forearm, elbow, wrist, forearmThickness);
        if (arm.elbow != null)
        {
            Vector3 along = (wrist - shoulder).sqrMagnitude > 0.0001f ? (elbow - shoulder).normalized + (wrist - elbow).normalized : Vector3.up;
            arm.elbow.SetPositionAndRotation(elbow, Quaternion.FromToRotation(Vector3.up, along.sqrMagnitude > 0.0001f ? along : Vector3.up));
        }
    }

    // A tube mesh that goes from y = 0 to y = 1, stretched between two points.
    static void Segment(Transform t, Vector3 from, Vector3 to, float thickness)
    {
        if (t == null) return;
        Vector3 d = to - from;
        float len = d.magnitude;
        t.SetPositionAndRotation(from, Quaternion.FromToRotation(Vector3.up, len > 0.0001f ? d / len : Vector3.up));
        t.localScale = new Vector3(thickness, Mathf.Max(0.001f, len), thickness);
    }

    static void SetLayer(Transform root, int layer)
    {
        if (root == null) return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }
}
