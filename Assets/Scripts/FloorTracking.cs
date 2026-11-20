using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

// Makes the headset measure height from the floor, so your view is at your real
// standing height instead of stuck at floor level.
public class FloorTracking : MonoBehaviour
{
    void Start()
    {
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (var s in subsystems)
            s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
    }
}
