using System.Collections.Generic;
using UnityEngine;

public enum CosmeticSlot { Head, Face, Neck, Back, Hands, BodyColor }

public enum BodyColorStyle { Solid, Rainbow, Glow }

// The "recipe card" for one cosmetic. Gorilla Company > 10. Generate Cosmetics makes all 30 of them.
// Shapes are placed relative to where they're worn:
//   Head / Face : the middle of your head (+Y up, +Z = the way you're looking)
//   Neck        : just under your chin
//   Back        : the middle of your body (+Z = front, so backpacks use negative Z)
//   Hands       : the middle of each hand
[CreateAssetMenu(menuName = "Gorilla Company/Cosmetic", fileName = "New Cosmetic")]
public class CosmeticData : ScriptableObject
{
    [Tooltip("Unique name used for saving. Don't change it after people have worn it.")]
    public string id = "new_cosmetic";
    public string displayName = "New Cosmetic";
    public CosmeticSlot slot = CosmeticSlot.Head;
    [TextArea(1, 3)] public string description;
    [Tooltip("Goop Coins. Everything is FREE (0) for now - this is here so some could be sold later.")]
    public int price = 0;

    [Header("Look (not used for Body Color)")]
    public List<PartRecipe> parts = new List<PartRecipe>();

    [Header("Body Color only")]
    public BodyColorStyle bodyStyle = BodyColorStyle.Solid;
    public Color bodyColor = Color.white;

    public bool IsFree => price <= 0;
}
