using UnityEngine;
using WebXR;

// The bridge to the WebXR Export add-on (used when the game runs in the Quest web browser).
// The add-on keeps its code in its own "box" that our normal scripts can't see, so this small box passes
// the controller buttons, sticks and positions on. Our scripts read them through VRInput.
// On PC / Quest app builds the add-on isn't running, so Active is false and nothing here is used.
public static class WebXRInput
{
    public struct Pad
    {
        public bool active;
        public Vector3 position;     // relative to the player's play area (like the Camera Offset)
        public Quaternion rotation;
        public float trigger, squeeze, stickPress, stickX, stickY, buttonA, buttonB;
    }

    public static Pad Left, Right;

    // true while you're actually in VR in the browser
    public static bool Active => WebXRManager.Instance != null && WebXRManager.Instance.XRState == WebXRState.VR;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        WebXRManager.OnControllerUpdate -= OnControllerUpdate;
        WebXRManager.OnControllerUpdate += OnControllerUpdate;
    }

    static void OnControllerUpdate(WebXRControllerData d)
    {
        var p = new Pad
        {
            active = d.enabled,
            position = d.gripPosition,
            rotation = d.gripRotation,
            trigger = d.trigger,
            squeeze = d.squeeze,
            stickPress = d.thumbstick,
            stickX = d.thumbstickX,
            stickY = d.thumbstickY,
            buttonA = d.buttonA,
            buttonB = d.buttonB,
        };
        if (d.hand == (int)WebXRControllerHand.LEFT) Left = p;
        else if (d.hand == (int)WebXRControllerHand.RIGHT) Right = p;
    }

    public static void Haptic(bool left, float strength, float seconds)
    {
        if (WebXRManager.Instance == null) return;
        WebXRManager.Instance.HapticPulse(left ? WebXRControllerHand.LEFT : WebXRControllerHand.RIGHT, strength, seconds * 1000f);
    }
}
