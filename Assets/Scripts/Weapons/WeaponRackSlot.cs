using UnityEngine;

// One spot on a weapon rack. When you take the weapon, a fresh copy appears here a few seconds later.
public class WeaponRackSlot : MonoBehaviour
{
    public GameObject prefab;
    public Grabbable current;
    public float refillDelay = 3f;

    float emptySince = -1f;

    void Update()
    {
        if (prefab == null) return;
        bool taken = current == null || current.HasBeenGrabbed;
        if (!taken)
        {
            emptySince = -1f;
            return;
        }
        if (emptySince < 0f) emptySince = Time.time;
        if (Time.time - emptySince >= refillDelay) Refill();
    }

    void Refill()
    {
        var go = Instantiate(prefab, transform.position, transform.rotation, transform);
        go.name = prefab.name;
        var body = go.GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true; // hangs on the rack until someone grabs it
        current = go.GetComponent<Grabbable>();
        emptySince = -1f;
    }
}
