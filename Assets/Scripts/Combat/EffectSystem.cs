using UnityEngine;

// Every weapon effect, written once and shared by all weapons.
//   Sticky = slow   Freeze = stop   Fire = damage over time   Shock = stun   Bounce = launch
//   Heal = HP back  Speed = faster  Shield = block damage     Teleport = blink a short way
//   Smoke = cloud   Silly = squeak + confetti
public static class EffectSystem
{
    // Effect on something that got hit.
    public static void ApplyToTarget(WeaponEffect effect, GameObject target, Vector3 point, Vector3 dir, WeaponData d)
    {
        if (target == null) return;
        var status = target.GetComponent<StatusEffects>();
        var health = target.GetComponent<Health>();

        switch (effect)
        {
            case WeaponEffect.Sticky:
                if (status) status.Slow(3f);
                FxSpawner.Burst(point, FxSpawner.GoopGreen, 6, 1.5f, 0.08f);
                SoundFx.Play(Sfx.Splat, point, 0.6f);
                break;
            case WeaponEffect.Fire:
                if (status) status.Burn(4f, 3f);
                FxSpawner.Burst(point, FxSpawner.FireOrange, 8, 2f, 0.08f);
                SoundFx.Play(Sfx.Sizzle, point, 0.6f);
                break;
            case WeaponEffect.Freeze:
                if (status) status.Freeze(1.5f);
                FxSpawner.Burst(point, FxSpawner.Ice, 8, 2f, 0.07f);
                SoundFx.Play(Sfx.Freeze, point, 0.6f);
                break;
            case WeaponEffect.Shock:
                if (status) status.Stun(0.8f);
                FxSpawner.Sparks(point);
                SoundFx.Play(Sfx.Zap, point, 0.6f);
                break;
            case WeaponEffect.Bounce:
                Push(target, Vector3.up * 6f + dir * 2f);
                if (status) status.Announce(WeaponEffect.Bounce, "BOING!", Color.magenta);
                SoundFx.Play(Sfx.Boing, point, 0.6f);
                break;
            case WeaponEffect.Heal:
                if (health) health.Heal(d != null ? d.damage : 25f);
                FxSpawner.Burst(point, FxSpawner.HealPink, 8, 1.5f, 0.06f);
                break;
            case WeaponEffect.Speed:
                if (status) status.SpeedBoost(5f);
                break;
            case WeaponEffect.Shield:
                if (health) health.Shield(4f);
                break;
            case WeaponEffect.Teleport:
                Vector3 flat = new Vector3(dir.x, 0f, dir.z);
                if (flat.sqrMagnitude < 0.01f) flat = Random.insideUnitSphere;
                flat.y = 0f;
                MoveBy(target, flat.normalized * 3f);
                FxSpawner.Burst(point, new Color(0.7f, 0.4f, 1f), 10, 2f, 0.06f);
                SoundFx.Play(Sfx.Whoosh, point);
                break;
            case WeaponEffect.Silly:
                if (status) status.Announce(WeaponEffect.Silly, "HONK!", Color.HSVToRGB(Random.value, 0.7f, 1f));
                break;
            // Smoke happens at the hit spot, see PlaceEffect
        }
    }

    // Effects that appear where the hit landed, even if nothing was hit.
    public static void PlaceEffect(WeaponEffect effect, Vector3 point)
    {
        if (effect == WeaponEffect.Smoke) FxSpawner.Smoke(point);
        else if (effect == WeaponEffect.Silly)
        {
            FxSpawner.Confetti(point);
            SoundFx.Play(Sfx.Squeak, point);
        }
    }

    // "Use" items on yourself: Healing Mango, Speed Juice, Jump Mushroom...
    public static void ApplyToSelf(WeaponData d, GameObject player, Transform hand)
    {
        if (player == null) return;
        var status = player.GetComponent<StatusEffects>();
        var health = player.GetComponent<Health>();
        Vector3 handPos = hand != null ? hand.position : player.transform.position + Vector3.up;
        Vector3 aim = hand != null ? hand.forward : player.transform.forward;
        Vector3 flatAim = new Vector3(aim.x, 0f, aim.z);
        flatAim = flatAim.sqrMagnitude > 0.01f ? flatAim.normalized : player.transform.forward;

        switch (d.effect)
        {
            case WeaponEffect.Heal:
                if (health) health.Heal(d.damage);
                FxSpawner.Burst(handPos, FxSpawner.HealPink, 12, 1.5f, 0.05f);
                SoundFx.Play(Sfx.Chime, handPos);
                break;
            case WeaponEffect.Speed:
                if (status) status.SpeedBoost(6f);
                FxSpawner.Burst(handPos, Color.cyan, 10, 2f, 0.05f);
                SoundFx.Play(Sfx.Whoosh, handPos);
                break;
            case WeaponEffect.Shield:
                if (health) health.Shield(5f);
                FxSpawner.Burst(handPos, FxSpawner.ShieldBlue, 12, 1.5f, 0.06f);
                SoundFx.Play(Sfx.Chime, handPos);
                break;
            case WeaponEffect.Bounce:
                // knockback = how high, range = how far forward (Grapple Vine pulls you where you point)
                Push(player, Vector3.up * d.knockback + aim.normalized * d.range, true);
                SoundFx.Play(Sfx.Boing, handPos);
                break;
            case WeaponEffect.Teleport:
                TeleportPlayer(player, flatAim, d.range > 0f ? d.range : 4f);
                break;
            case WeaponEffect.Smoke:
                FxSpawner.Smoke(player.transform.position + Vector3.up * 0.5f);
                break;
            case WeaponEffect.Silly:
                FxSpawner.Confetti(handPos);
                SoundFx.Play(Sfx.Squeak, handPos);
                break;
            default:
                ApplyToTarget(d.effect, player, handPos, Vector3.up, d);
                break;
        }
    }

    // Push something with physics (does nothing to things that can't move, like dummies).
    public static void Push(GameObject target, Vector3 velocityChange, bool replaceVertical = false)
    {
        var body = target.GetComponent<Rigidbody>();
        if (body == null || body.isKinematic || velocityChange == Vector3.zero) return;
        Vector3 v = body.linearVelocity;
        if (replaceVertical || velocityChange.y > 0f) v.y = Mathf.Max(v.y, 0f);
        body.linearVelocity = v + velocityChange;
    }

    static void MoveBy(GameObject target, Vector3 offset)
    {
        var body = target.GetComponent<Rigidbody>();
        Vector3 p = target.transform.position + offset;
        if (body != null && !body.isKinematic) body.position = p;
        target.transform.position = p;
    }

    // Blink forward, stopping before walls.
    static void TeleportPlayer(GameObject player, Vector3 dir, float distance)
    {
        Vector3 from = player.transform.position + Vector3.up * 1f;
        if (Physics.SphereCast(from, 0.3f, dir, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            distance = Mathf.Max(0f, hit.distance - 0.4f);
        FxSpawner.Burst(from, new Color(0.7f, 0.4f, 1f), 12, 2f, 0.06f);
        MoveBy(player, dir * distance);
        SoundFx.Play(Sfx.Whoosh, player.transform.position);
    }

    // Goop Wall Spray: a climbable green wall in front of you that melts after 20 seconds.
    public static void SpawnGoopWall(GameObject player, Transform hand)
    {
        Vector3 fwd = hand != null ? hand.forward : player.transform.forward;
        fwd.y = 0f;
        fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
        Vector3 pos = player.transform.position + fwd * 1.5f + Vector3.up * 1f;

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Goop Wall";
        wall.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd));
        wall.transform.localScale = new Vector3(1.6f, 2f, 0.3f);
        wall.AddComponent<PrimitiveColor>().SetColor(FxSpawner.GoopGreen);
        Object.Destroy(wall, 20f);
        FxSpawner.Burst(pos, FxSpawner.GoopGreen, 14, 2f, 0.1f);
        SoundFx.Play(Sfx.Splat, pos);
    }
}
