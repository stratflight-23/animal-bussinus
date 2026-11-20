using System.Collections.Generic;
using UnityEngine;

// A spike trap in the floor. The spikes go down (safe), peek up as a warning, then shoot up (ouch).
// While they're up, anything with Health standing on it takes damage through the Health system
// (so the spawn safe zone and shields still work like everywhere else).
// Needs a trigger collider covering the spike area. "spikes" = the spike shapes that move up and down.
[RequireComponent(typeof(Collider))]
public class SpikeFloor : MonoBehaviour
{
    public float damage = 15f;
    [Tooltip("Seconds between hits while you stand on the spikes.")]
    public float hitEvery = 0.6f;
    public float downSeconds = 1.6f;
    public float warnSeconds = 0.5f;
    public float upSeconds = 1.2f;
    [Tooltip("Shift the timing so neighbouring spike floors don't all pop at once.")]
    public float timeOffset;

    public Transform spikes;
    [Tooltip("How far the spikes sink into the floor when they're down.")]
    public float sinkDepth = 0.45f;

    public bool IsUp { get; private set; }

    readonly Dictionary<Health, float> lastHit = new Dictionary<Health, float>();

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void Update()
    {
        float total = downSeconds + warnSeconds + upSeconds;
        float t = Mathf.Repeat(Time.time + timeOffset, total);
        float targetY;
        bool up = false;
        if (t < downSeconds) targetY = -sinkDepth;
        else if (t < downSeconds + warnSeconds) targetY = -sinkDepth * 0.65f;  // little peek = warning
        else { targetY = 0f; up = true; }

        if (up && !IsUp) SoundFx.Play(Sfx.Whoosh, transform.position, 0.35f);
        IsUp = up;

        if (spikes != null)
        {
            Vector3 p = spikes.localPosition;
            p.y = Mathf.MoveTowards(p.y, targetY, Time.deltaTime * (up ? 8f : 2f));
            spikes.localPosition = p;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!IsUp) return;
        var health = other.GetComponentInParent<Health>();
        if (health == null || health.IsDead) return;
        if (lastHit.TryGetValue(health, out float when) && Time.time - when < hitEvery) return;
        lastHit[health] = Time.time;

        Vector3 point = other.bounds.center;
        if (health.TakeDamage(damage, point, Vector3.up))
        {
            EffectSystem.Push(health.gameObject, Vector3.up * 2f);
            FxSpawner.Burst(point, new Color(0.9f, 0.9f, 0.95f), 5, 1.5f, 0.04f);
        }
    }
}
