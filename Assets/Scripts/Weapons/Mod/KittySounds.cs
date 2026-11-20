using UnityEngine;

// Cute cat sounds made from math (no audio files, nothing copied).
// If you drop a real AudioClip into the Kitty Blaster's sound slots, that one is used instead.
public static class KittySounds
{
    const int Rate = 22050;
    static AudioClip mrrp, mew, hiss, scratch;
    static readonly System.Random rng = new System.Random(927);

    // Plays "custom" if one was given, otherwise the made-from-math sound.
    public static void Play(AudioClip custom, AudioClip fallback, Vector3 position, float volume = 0.7f)
    {
        var clip = custom != null ? custom : fallback;
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    // A happy rolling "mrrp?" that goes up at the end.
    public static AudioClip Mrrp
    {
        get
        {
            if (mrrp == null)
                mrrp = Make("Mrrp", 0.28f, (t, k) =>
                {
                    float freq = 430f + 280f * k * k;
                    float trill = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 30f * t); // the rolling "rr"
                    return Voice(t, freq) * trill * Env(k, 0.12f);
                });
            return mrrp;
        }
    }

    // A small sad "mew" that slides down.
    public static AudioClip Mew
    {
        get
        {
            if (mew == null)
                mew = Make("Mew", 0.5f, (t, k) =>
                {
                    float freq = 820f - 330f * k + 18f * Mathf.Sin(2f * Mathf.PI * 6f * t); // little wobble
                    return Voice(t, freq) * Env(k, 0.2f) * 0.8f;
                });
            return mew;
        }
    }

    // A soft "hhhsss" (filtered noise).
    public static AudioClip Hiss
    {
        get
        {
            if (hiss == null)
            {
                float lp = 0f;
                hiss = Make("Hiss", 0.45f, (t, k) =>
                {
                    float noise = (float)rng.NextDouble() * 2f - 1f;
                    lp += (noise - lp) * 0.35f;
                    return (noise - lp) * 0.9f * Mathf.Sqrt(Mathf.Sin(Mathf.PI * k));
                });
            }
            return hiss;
        }
    }

    // Three quick "scritch" sounds.
    public static AudioClip Scratch
    {
        get
        {
            if (scratch == null)
                scratch = Make("Scratch", 0.2f, (t, k) =>
                {
                    float noise = (float)rng.NextDouble() * 2f - 1f;
                    float part = (k * 3f) % 1f;            // 3 little bursts
                    return noise * Mathf.Pow(1f - part, 3f) * 0.6f;
                });
            return scratch;
        }
    }

    // ------------------------------------------------------------ the math

    static float voicePhase;

    // A "meow-ish" voice: a main tone plus a couple of softer higher ones.
    static float Voice(float t, float freq)
    {
        voicePhase += 2f * Mathf.PI * freq / Rate;
        return Mathf.Sin(voicePhase) * 0.6f + Mathf.Sin(voicePhase * 2f) * 0.3f + Mathf.Sin(voicePhase * 3f) * 0.15f;
    }

    static AudioClip Make(string name, float seconds, System.Func<float, float, float> sample)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        voicePhase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            data[i] = Mathf.Clamp(sample(t, t / seconds) * 0.7f, -1f, 1f);
        }
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // quick fade in, slow fade out
    static float Env(float k, float attack) => k < attack ? k / attack : 1f - (k - attack) / (1f - attack);
}
