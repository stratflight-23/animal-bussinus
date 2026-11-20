using System.Collections.Generic;
using UnityEngine;

public enum Sfx { Squeak, Boom, Zap, Pew, Bonk, Whoosh, Chime, Splat, Sizzle, Freeze, Boing, Poof }

// Cartoon sound effects made from math at runtime (no audio files needed, nothing copied).
public static class SoundFx
{
    const int Rate = 22050;
    static readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    static AudioClip copterLoop;
    static System.Random rng = new System.Random(1234);

    public static void Play(Sfx sound, Vector3 position, float volume = 0.8f)
    {
        AudioSource.PlayClipAtPoint(Get(sound), position, volume);
    }

    public static AudioClip Get(Sfx sound)
    {
        if (clips.TryGetValue(sound, out var c) && c != null) return c;
        c = Make(sound);
        clips[sound] = c;
        return c;
    }

    // A looping "whup-whup-whirr" for the Goop Copter.
    public static AudioClip CopterLoop
    {
        get
        {
            if (copterLoop != null) return copterLoop;
            int n = Rate; // exactly 1 second so it loops cleanly
            var data = new float[n];
            float lowNoise = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float chop = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Mathf.PI * 7f * t)); // 7 whups per second
                float buzz = Mathf.Sin(2 * Mathf.PI * 55f * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 110f * t) * 0.25f
                           + Mathf.Sin(2 * Mathf.PI * 165f * t) * 0.12f;
                lowNoise += ((float)rng.NextDouble() * 2f - 1f - lowNoise) * 0.08f;
                data[i] = (buzz * 0.6f + lowNoise * 1.5f) * chop * 0.5f;
            }
            copterLoop = AudioClip.Create("CopterLoop", n, 1, Rate, false);
            copterLoop.SetData(data, 0);
            return copterLoop;
        }
    }

    static AudioClip Make(Sfx sound)
    {
        float seconds;
        switch (sound)
        {
            case Sfx.Boom: seconds = 0.7f; break;
            case Sfx.Chime: seconds = 0.6f; break;
            case Sfx.Boing: seconds = 0.45f; break;
            case Sfx.Poof: seconds = 0.5f; break;
            case Sfx.Sizzle: seconds = 0.45f; break;
            default: seconds = 0.25f; break;
        }

        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        float phase = 0f, lp = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float k = t / seconds;             // 0 -> 1 through the sound
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float s = 0f, freq;

            switch (sound)
            {
                case Sfx.Squeak: // rubber-toy squeak: up then down with wobble
                    freq = 900f + 700f * Mathf.Sin(Mathf.PI * k) + 60f * Mathf.Sin(2 * Mathf.PI * 30f * t);
                    phase += 2 * Mathf.PI * freq / Rate;
                    s = (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f)) * Env(k, 0.05f);
                    break;
                case Sfx.Boom:
                    lp += (noise - lp) * (0.12f - 0.1f * k);
                    phase += 2 * Mathf.PI * Mathf.Lerp(90f, 35f, k) / Rate;
                    s = (lp * 3f + Mathf.Sin(phase) * 0.6f) * Mathf.Pow(1f - k, 2f);
                    break;
                case Sfx.Zap:
                    phase += 2 * Mathf.PI * (140f + noise * 80f) / Rate;
                    s = (Mathf.Sign(Mathf.Sin(phase)) * 0.5f + noise * 0.4f) * (1f - k) * (rng.NextDouble() > 0.15 ? 1f : 0f);
                    break;
                case Sfx.Pew:
                    phase += 2 * Mathf.PI * Mathf.Lerp(1400f, 250f, k) / Rate;
                    s = Mathf.Sin(phase) * (1f - k);
                    break;
                case Sfx.Bonk:
                    phase += 2 * Mathf.PI * Mathf.Lerp(260f, 90f, k) / Rate;
                    s = (Mathf.Sin(phase) + (k < 0.04f ? noise * 0.8f : 0f)) * Mathf.Pow(1f - k, 3f);
                    break;
                case Sfx.Whoosh:
                    lp += (noise - lp) * 0.15f;
                    s = lp * 2.5f * Mathf.Sin(Mathf.PI * k);
                    break;
                case Sfx.Chime:
                    s = (Mathf.Sin(2 * Mathf.PI * 880f * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * 1320f * t)
                         + 0.4f * Mathf.Sin(2 * Mathf.PI * 1760f * t * (k > 0.3f ? 1f : 0f))) * 0.5f * Mathf.Pow(1f - k, 2f);
                    break;
                case Sfx.Splat:
                    lp += (noise - lp) * 0.25f;
                    s = lp * 2.5f * Mathf.Pow(1f - k, 3f);
                    break;
                case Sfx.Sizzle:
                    s = noise * 0.5f * (0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * 40f * t)) * (1f - k);
                    break;
                case Sfx.Freeze:
                    phase += 2 * Mathf.PI * Mathf.Lerp(2400f, 1800f, k) / Rate;
                    s = (Mathf.Sin(phase) * 0.4f + noise * 0.2f) * (1f - k) * (Mathf.Sin(2 * Mathf.PI * 50f * t) > 0 ? 1f : 0.3f);
                    break;
                case Sfx.Boing: // spring
                    freq = 180f + 120f * Mathf.Sin(2 * Mathf.PI * 14f * t) * (1f - k);
                    phase += 2 * Mathf.PI * freq / Rate;
                    s = Mathf.Sin(phase) * (1f - k);
                    break;
                case Sfx.Poof:
                    lp += (noise - lp) * 0.05f;
                    s = lp * 4f * Env(k, 0.1f);
                    break;
            }
            data[i] = Mathf.Clamp(s * 0.7f, -1f, 1f);
        }

        var clip = AudioClip.Create(sound.ToString(), n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // quick fade in, slow fade out
    static float Env(float k, float attack) => k < attack ? k / attack : 1f - (k - attack) / (1f - attack);
}
