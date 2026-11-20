using UnityEngine;

// An empty marker for where a monster will appear later (no monsters yet!).
// Shows as a red ball in the Scene view so you can find and move them.
public class MonsterSpawn : MonoBehaviour
{
    public string roomName;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
