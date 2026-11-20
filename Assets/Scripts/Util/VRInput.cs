using UnityEngine;
using UnityEngine.XR;

// ONE place that reads the VR controllers, so the game works the same everywhere:
//   - Quest app / PC VR: Unity's normal VR input (OpenXR)
//   - Quest web browser (WebXR): the WebXR Export add-on, through WebXRInput
// Buttons: Grip (squeeze), Trigger, Primary (A / X), Secondary (B / Y), StickClick. Stick = thumbstick.
public static class VRInput
{
    static bool Web => WebXRInput.Active;
    static WebXRInput.Pad Pad(XRNode hand) => hand == XRNode.LeftHand ? WebXRInput.Left : WebXRInput.Right;

    public static bool Grip(XRNode hand) => Web ? Pad(hand).squeeze > 0.5f : GetBool(hand, CommonUsages.gripButton);
    public static bool Trigger(XRNode hand) => Web ? Pad(hand).trigger > 0.5f : GetBool(hand, CommonUsages.triggerButton);
    public static bool Primary(XRNode hand) => Web ? Pad(hand).buttonA > 0.5f : GetBool(hand, CommonUsages.primaryButton);
    public static bool Secondary(XRNode hand) => Web ? Pad(hand).buttonB > 0.5f : GetBool(hand, CommonUsages.secondaryButton);
    public static bool StickClick(XRNode hand) => Web ? Pad(hand).stickPress > 0.5f : GetBool(hand, CommonUsages.primary2DAxisClick);

    // Thumbstick: x = right, y = forward/up (both -1..1). Returns false if there's no controller.
    public static bool Stick(XRNode hand, out Vector2 value)
    {
        if (Web)
        {
            var p = Pad(hand);
            value = new Vector2(p.stickX, p.stickY);
            return p.active;
        }
        return InputDevices.GetDeviceAtXRNode(hand).TryGetFeatureValue(CommonUsages.primary2DAxis, out value);
    }

    public static Vector2 Stick(XRNode hand) => Stick(hand, out Vector2 v) ? v : Vector2.zero;

    // Little vibration in the controller.
    public static void Buzz(XRNode hand, float strength, float seconds)
    {
        if (Web) WebXRInput.Haptic(hand == XRNode.LeftHand, strength, seconds);
        else InputDevices.GetDeviceAtXRNode(hand).SendHapticImpulse(0u, strength, seconds);
    }

    static bool GetBool(XRNode hand, InputFeatureUsage<bool> usage) =>
        InputDevices.GetDeviceAtXRNode(hand).TryGetFeatureValue(usage, out bool v) && v;
}

// In the Quest web browser: a backup for head tracking. If the normal tracking component isn't moving the camera,
// this reads the headset pose straight from Unity's XR devices. Added to the camera automatically on the web.
public class WebXRHeadFollower : MonoBehaviour
{
    UnityEngine.InputSystem.XR.TrackedPoseDriver tpd;

    void Awake() => tpd = GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
    void OnEnable() => Application.onBeforeRender += Follow;
    void OnDisable() => Application.onBeforeRender -= Follow;
    void Update() => Follow();

    void Follow()
    {
        if (!WebXRInput.Active) return;
        var head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        if (!head.isValid) head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        bool hasPos = head.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 pos)
                      || head.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
        bool hasRot = head.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rot)
                      || head.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);
        if (!hasPos || !hasRot) return;            // nothing here: leave it to the normal tracking
        if (tpd != null && tpd.enabled) tpd.enabled = false;
        transform.localPosition = pos;
        transform.localRotation = rot;
    }
}

// In the Quest web browser, the hands get their position from WebXR instead of the normal tracking.
// Added to "Left Hand" / "Right Hand" automatically when the game runs as a web page.
public class WebXRHandFollower : MonoBehaviour
{
    XRNode node;

    void Awake()
    {
        node = name.ToLower().Contains("left") ? XRNode.LeftHand : XRNode.RightHand;
        // the normal tracking component can't see WebXR controllers, so it's turned off here
        var tpd = GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (tpd != null) tpd.enabled = false;
    }

    void OnEnable() => Application.onBeforeRender += Follow;
    void OnDisable() => Application.onBeforeRender -= Follow;
    void Update() => Follow();

    void Follow()
    {
        if (!WebXRInput.Active) return;
        var p = node == XRNode.LeftHand ? WebXRInput.Left : WebXRInput.Right;
        if (!p.active) return;
        transform.localPosition = p.position;
        transform.localRotation = p.rotation;
    }
}
