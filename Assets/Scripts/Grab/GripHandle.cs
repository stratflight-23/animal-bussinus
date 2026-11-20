using System;
using UnityEngine;

// Something you can grip that does NOT come off when you grab it (like the Goop Copter's levers).
// Needs a collider (a trigger is fine). Other scripts listen to Gripped / Released.
public class GripHandle : MonoBehaviour
{
    public event Action<HandGrabber> Gripped;
    public event Action<HandGrabber> Released;

    public void NotifyGripped(HandGrabber hand) => Gripped?.Invoke(hand);
    public void NotifyReleased(HandGrabber hand) => Released?.Invoke(hand);
}
