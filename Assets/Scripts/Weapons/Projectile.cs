using UnityEngine;

// A shot fired by a Shoot weapon. Made from a simple shape at runtime.
public class Projectile : MonoBehaviour
{
    WeaponData data;
    GameObject owner;
    Rigidbody rb;
    float dieAt;
    bool done;
    Vector3 lastVelocity;

    public static Projectile Fire(WeaponData d, Vector3 position, Vector3 velocity, GameObject owner)
    {
        var go = GameObject.CreatePrimitive(d.projectileShape);
        go.name = d.weaponName + " Shot";
        go.transform.position = position;
        Quaternion look = Quaternion.LookRotation(velocity.sqrMagnitude > 0.01f ? velocity : Vector3.forward);
        float s = d.projectileSize;
        bool longShape = d.projectileShape == PrimitiveType.Capsule || d.projectileShape == PrimitiveType.Cylinder;
        go.transform.rotation = longShape ? look * Quaternion.Euler(90f, 0f, 0f) : look;
        go.transform.localScale = longShape ? new Vector3(s * 0.5f, s, s * 0.5f) : Vector3.one * s;
        go.AddComponent<PrimitiveColor>().SetColor(d.accentColor);

        var body = go.AddComponent<Rigidbody>();
        body.mass = 0.2f;
        body.useGravity = d.projectileGravity;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.linearVelocity = velocity;

        if (owner != null)
        {
            var mine = go.GetComponent<Collider>();
            foreach (var c in owner.GetComponentsInChildren<Collider>())
                if (c.enabled) Physics.IgnoreCollision(mine, c);
        }

        if (d.glows)
        {
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = d.accentColor;
            light.range = 2.5f;
            light.intensity = 2f;
        }

        var p = go.AddComponent<Projectile>();
        p.data = d;
        p.owner = owner;
        p.rb = body;
        p.lastVelocity = velocity;
        p.dieAt = Time.time + Mathf.Clamp(d.range / Mathf.Max(1f, d.projectileSpeed), 0.4f, 6f);
        return p;
    }

    void FixedUpdate()
    {
        lastVelocity = rb.linearVelocity;
    }

    void Update()
    {
        if (done || Time.time < dieAt) return;
        done = true;
        if (data.areaRadius > 0f) Combat.Impact(data, null, transform.position, lastVelocity, owner, gameObject);
        else EffectSystem.PlaceEffect(data.effect, transform.position);
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (done) return;
        done = true;
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Combat.Impact(data, collision.collider, point, lastVelocity, owner, gameObject);
        Destroy(gameObject);
    }
}
