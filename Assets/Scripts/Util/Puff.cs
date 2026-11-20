using UnityEngine;

// One effect blob: grows/shrinks, can fly with gravity, then disappears.
public class Puff : MonoBehaviour
{
    public float startSize, endSize, life, holdFraction;
    public Vector3 velocity, spin;
    public bool gravity, flat, keepShape;
    float age;

    public static Puff Create(PrimitiveType type, Vector3 pos, Color color, float startSize, float endSize, float life,
                              Vector3 velocity, bool gravity)
    {
        var go = PrimitiveColor.MakeShape(type, color, "FX");
        go.transform.position = pos;
        var p = go.AddComponent<Puff>();
        p.startSize = startSize;
        p.endSize = endSize;
        p.life = life;
        p.velocity = velocity;
        p.gravity = gravity;
        p.Apply(0f);
        return p;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (gravity) velocity += Physics.gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        if (spin != Vector3.zero) transform.Rotate(spin * Time.deltaTime);
        float k = age / life;
        Apply(k);
        if (k >= 1f) Destroy(gameObject);
    }

    void Apply(float k)
    {
        if (keepShape)
        {
            if (k > 0.8f) transform.localScale *= 0.9f;
            return;
        }
        float s;
        if (holdFraction > 0f)
            s = k < 0.15f ? Mathf.Lerp(startSize, endSize, k / 0.15f)
              : k < holdFraction ? endSize
              : Mathf.Lerp(endSize, 0f, (k - holdFraction) / (1f - holdFraction));
        else
            s = Mathf.Lerp(startSize, endSize, k) * (k > 0.7f ? (1f - k) / 0.3f : 1f);
        transform.localScale = flat ? new Vector3(s, 0.01f, s) : Vector3.one * s;
    }
}
