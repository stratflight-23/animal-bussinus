using UnityEngine;
using UnityEngine.XR;

// Put this on each VR hand. Lets the hand press PokeButtons and buzzes the controller when it does.
public class HandPoker : MonoBehaviour
{
    [Tooltip("Size of the touch area (in the hand's own scale).")]
    public float radius = 0.5f;

    XRNode node;

    void Awake()
    {
        var col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;

        node = name.ToLower().Contains("left") ? XRNode.LeftHand : XRNode.RightHand;
    }

    public void Buzz(float strength = 0.4f, float seconds = 0.05f)
    {
        VRInput.Buzz(node, strength, seconds);
    }
}
