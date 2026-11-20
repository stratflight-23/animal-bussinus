using System.Collections.Generic;
using UnityEngine;

// A stone platform that slides back and forth (like across the lava pit), and carries whoever stands on it.
// moveOffset = how far it travels from where it starts. It waits a moment at each end.
[RequireComponent(typeof(Rigidbody))]
public class TempleMover : MonoBehaviour
{
    public Vector3 moveOffset = new Vector3(2f, 0f, 0f);
    public float secondsPerTrip = 3f;
    public float pauseAtEnds = 1f;
    public float timeOffset;

    Rigidbody rb;
    Vector3 start;
    readonly HashSet<Rigidbody> riders = new HashSet<Rigidbody>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        start = transform.position;
    }

    void FixedUpdate()
    {
        float trip = Mathf.Max(0.1f, secondsPerTrip);
        float cycle = 2f * (trip + pauseAtEnds);
        float t = Mathf.Repeat(Time.time + timeOffset, cycle);
        float k;
        if (t < pauseAtEnds) k = 0f;
        else if (t < pauseAtEnds + trip) k = Mathf.SmoothStep(0f, 1f, (t - pauseAtEnds) / trip);
        else if (t < 2f * pauseAtEnds + trip) k = 1f;
        else k = 1f - Mathf.SmoothStep(0f, 1f, (t - 2f * pauseAtEnds - trip) / trip);

        Vector3 target = start + moveOffset * k;
        Vector3 delta = target - rb.position;
        rb.MovePosition(target);

        // carry riders along (players, dropped loot...)
        foreach (var r in riders)
            if (r != null && !r.isKinematic) r.position += delta;
        riders.Clear(); // refilled by OnCollisionStay every physics step
    }

    void OnCollisionStay(Collision collision)
    {
        var body = collision.rigidbody;
        if (body == null) return;
        for (int i = 0; i < collision.contactCount; i++)
            if (collision.GetContact(i).normal.y < -0.5f) // they're on top of us
            {
                riders.Add(body);
                return;
            }
    }
}
