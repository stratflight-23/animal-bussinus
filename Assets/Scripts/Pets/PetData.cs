using System.Collections.Generic;
using UnityEngine;

public enum PetTrick { Sit, Spin, Hop, Wag }

// The "recipe card" for one pet. Gorilla Company > 12. Generate Pets makes all 10 of them.
// Shapes are placed with the pet's feet at (0,0,0) and its nose pointing +Z.
// Name a shape "Tail" to make it wag (it swings around "Tail Pivot"), or "Wing" to make it flap.
[CreateAssetMenu(menuName = "Gorilla Company/Pet", fileName = "New Pet")]
public class PetData : ScriptableObject
{
    [Tooltip("Unique name used for saving. Don't change it after people have used it.")]
    public string id = "new_pet";
    public string displayName = "New Pet";
    [TextArea(1, 3)] public string description;
    [Tooltip("Goop Coins. Every pet is FREE (0) for now.")]
    public int price = 0;

    [Header("Look")]
    public List<PartRecipe> parts = new List<PartRecipe>();
    [Tooltip("Makes the whole pet bigger or smaller.")]
    public float size = 1f;
    [Tooltip("Where the tail is attached (the tail swings around this point).")]
    public Vector3 tailPivot = new Vector3(0f, 0.15f, -0.15f);

    [Header("Moving")]
    [Tooltip("Walking speed in meters per second (it speeds up when it's far behind).")]
    public float followSpeed = 3f;
    [Tooltip("Flies instead of walking (floats near your shoulder).")]
    public bool flies;
    [Tooltip("Always bounces as it moves (like a slime).")]
    public bool bouncy;

    [Header("Idle trick (when you stand still)")]
    public PetTrick idleTrick = PetTrick.Sit;
}
