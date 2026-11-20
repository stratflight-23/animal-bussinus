using System;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType { Swing, Stab, Throw, Shoot, Trap, Use }

public enum WeaponEffect { None, Sticky, Fire, Freeze, Shock, Bounce, Heal, Speed, Shield, Teleport, Smoke, Silly }

// One shape in a weapon's look (a cube, sphere, capsule or cylinder).
[Serializable]
public class ShapePart
{
    public PrimitiveType shape = PrimitiveType.Cube;
    [Tooltip("The shape's scale. Cylinders and capsules are 2 units tall at scale 1.")]
    public Vector3 size = Vector3.one * 0.1f;
    [Tooltip("Position from the grip point. +Z points away from your hand.")]
    public Vector3 offset;
    [Tooltip("Rotation in degrees.")]
    public Vector3 rotation;
    public Color color = Color.white;
}

// The "recipe card" for one weapon. Gorilla Company > 4. Generate All Weapons makes 100 of these.
// Change numbers here to rebalance a weapon (no code needed).
[CreateAssetMenu(menuName = "Gorilla Company/Weapon Data", fileName = "New Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "New Weapon";
    public string category = "Bonkers";
    [TextArea(2, 4)] public string description;
    [Tooltip("Price in Goop Coins.")]
    public int price = 100;

    [Header("Attack")]
    public AttackType attackType = AttackType.Swing;
    [Tooltip("Damage per hit. For Heal items this is how much HP it gives back.")]
    public float damage = 10f;
    [Tooltip("Seconds between attacks / shots / uses.")]
    public float cooldown = 0.5f;
    [Tooltip("Swing/Stab: size of the hit area at the tip.  Shoot: how far shots fly.  Trap: trigger radius.\n" +
             "Use (area): radius.  Use + Teleport: distance.  Use + Bounce: forward push.")]
    public float range = 0.2f;
    public float projectileSpeed = 20f;
    [Tooltip("How hard targets get pushed. Negative = pulls them toward you. Use + Bounce: upward launch speed.")]
    public float knockback = 3f;
    public WeaponEffect effect = WeaponEffect.None;

    [Header("Extras")]
    [Tooltip("If above 0, hits explode and affect everything in this radius.")]
    public float areaRadius;
    [Tooltip("Used up after one throw / use / trap trigger.")]
    public bool singleUse;
    [Tooltip("Use items: affect everyone around you (radius = range) instead of yourself.")]
    public bool affectsArea;
    [Tooltip("Thrown item flies back toward you (boomerang).")]
    public bool returnsAfterThrow;
    [Tooltip("Use item: places a Goop wall in front of you.")]
    public bool spawnsBlock;
    [Tooltip("Has a little light. Use items with effect None toggle it on/off.")]
    public bool glows;
    public bool projectileGravity;
    public PrimitiveType projectileShape = PrimitiveType.Sphere;
    public float projectileSize = 0.12f;
    public float mass = 1f;

    [Header("Look")]
    public Color mainColor = Color.white;
    public Color accentColor = Color.gray;
    public List<ShapePart> shape = new List<ShapePart>();

    [Header("Filled in by the generator")]
    public int catalogIndex;
    public float length = 0.5f;
    public Vector3 tipOffset = new Vector3(0f, 0f, 0.5f);
}
