using UnityEngine;

namespace BitArena
{
    public class SfxSynth : MonoBehaviour
    {
        static SfxSynth instance;
        static AudioClip jumpClip;
        static AudioClip shootClip;
        static AudioClip swingClip;
        static AudioClip killClip;
        static AudioClip wrongClip;
        static AudioClip hurtClip;
        static AudioClip swapClip;
        static AudioClip winClip;
        static AudioClip loseClip;
        static AudioClip healClip;
        static AudioClip teleportClip;
        static AudioClip menuTickClip;

        AudioSource source;

        public static void Init(GameObject host)
        {
            var synth = host.AddComponent<SfxSynth>();
            synth.source = host.AddComponent<AudioSource>();
            synth.source.playOnAwake = false;
            synth.source.spatialBlend = 0f;
            instance = synth;
            if (jumpClip == null)
            {
                jumpClip = Tone(620f, 0.09f, 0.18f, false);
                shootClip = Tone(880f, 0.07f, 0.16f, true);
                swingClip = Noise(0.09f, 0.2f, 180f);
                killClip = Chord(520f, 780f, 0.16f, 0.2f);
                wrongClip = Tone(140f, 0.12f, 0.2f, true);
                hurtClip = Noise(0.14f, 0.28f, 70f);
                swapClip = Chord(440f, 660f, 0.14f, 0.18f);
                winClip = Chord(523f, 784f, 0.28f, 0.2f);
                loseClip = Tone(180f, 0.35f, 0.22f, true);
                healClip = HealArpeggio(0.24f, 0.22f);
                teleportClip = Tone(720f, 0.11f, 0.12f, true);
                menuTickClip = Tone(880f, 0.04f, 0.14f, false);
            }
        }

        public static void Jump() => Play(jumpClip);
        public static void Shoot() => Play(shootClip);
        public static void Swing() => Play(swingClip);
        public static void Kill() => Play(killClip);
        public static void Wrong() => Play(wrongClip);
        public static void Hurt() => Play(hurtClip);
        public static void Swap() => Play(swapClip);
        public static void Win() => Play(winClip);
        public static void Lose() => Play(loseClip);
        public static void Heal() => Play(healClip);
        public static void ItemTeleport() => Play(teleportClip);
        public static void MenuTick() => Play(menuTickClip);

        static void Play(AudioClip clip)
        {
            if (instance != null && instance.source != null && clip != null)
                instance.source.PlayOneShot(clip);
        }

        static AudioClip Tone(float freq, float duration, float volume, bool drop)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(rate * duration);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float k = i / (float)count;
                float f = drop ? Mathf.Lerp(freq, freq * 0.45f, k) : freq;
                float env = (1f - k) * (1f - k);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * volume;
            }
            return ClipFrom(data, rate);
        }

        static AudioClip HealArpeggio(float duration, float volume)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(rate * duration);
            var data = new float[count];
            float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float k = i / (float)count;
                int noteIdx = Mathf.Clamp((int)(k * 4f), 0, 3);
                float f = freqs[noteIdx];
                float noteProgress = (k * 4f) - noteIdx;
                float env = (1f - noteProgress * 0.6f) * (1f - k * 0.5f);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * volume;
            }
            return ClipFrom(data, rate);
        }

        static AudioClip Chord(float a, float b, float duration, float volume)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(rate * duration);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float k = i / (float)count;
                float env = (1f - k) * (1f - k);
                float sample = Mathf.Sin(2f * Mathf.PI * a * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * b * t) * 0.4f;
                data[i] = sample * env * volume;
            }
            return ClipFrom(data, rate);
        }

        static AudioClip Noise(float duration, float volume, float tone)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(rate * duration);
            var data = new float[count];
            var rng = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float k = i / (float)count;
                float env = (1f - k);
                float n = (float)rng.NextDouble() * 2f - 1f;
                data[i] = (n * 0.65f + Mathf.Sin(2f * Mathf.PI * tone * t) * 0.35f) * env * volume;
            }
            return ClipFrom(data, rate);
        }

        static AudioClip ClipFrom(float[] data, int rate)
        {
            var clip = AudioClip.Create("bit-sfx", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
