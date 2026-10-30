using UnityEngine;

namespace VRAssignment
{
    /// <summary>
    /// Fallback sound effects generated in code, used whenever a component has no AudioClip assigned.
    /// The scenes assign the Create with VR course sounds in the Inspector; these are only a backup.
    /// </summary>
    public static class KitAudio
    {
        const int k_SampleRate = 44100;

        static AudioClip s_Click;
        static AudioClip s_Snap;
        static AudioClip s_Success;
        static AudioClip s_Creak;

        public static AudioClip Click => s_Click != null ? s_Click : s_Click = Tone("Kit Click", 0.035f, 1800f, 0f, 60f);
        public static AudioClip Snap => s_Snap != null ? s_Snap : s_Snap = Tone("Kit Snap", 0.09f, 520f, 380f, 35f);

        public static AudioClip Success
        {
            get
            {
                if (s_Success != null)
                    return s_Success;

                float[] notes = { 523.25f, 659.25f, 783.99f };
                const float noteLength = 0.12f;
                var data = new float[(int)(k_SampleRate * noteLength * notes.Length)];
                for (var i = 0; i < data.Length; i++)
                {
                    var note = Mathf.Min(i / (int)(k_SampleRate * noteLength), notes.Length - 1);
                    var t = (i % (int)(k_SampleRate * noteLength)) / (float)k_SampleRate;
                    data[i] = Mathf.Sin(2f * Mathf.PI * notes[note] * t) * Mathf.Exp(-t * 14f) * 0.35f;
                }

                s_Success = AudioClip.Create("Kit Success", data.Length, 1, k_SampleRate, false);
                s_Success.SetData(data, 0);
                return s_Success;
            }
        }

        /// <summary>Looping low hinge squeak, volume is driven by how fast the door swings.</summary>
        public static AudioClip Creak
        {
            get
            {
                if (s_Creak != null)
                    return s_Creak;

                var data = new float[k_SampleRate];
                var random = new System.Random(7);
                for (var i = 0; i < data.Length; i++)
                {
                    var t = i / (float)k_SampleRate;
                    var wobble = 1f + 0.08f * Mathf.Sin(2f * Mathf.PI * 3f * t);
                    var saw = 2f * Mathf.Repeat(t * 140f * wobble, 1f) - 1f;
                    var noise = (float)random.NextDouble() * 2f - 1f;
                    data[i] = (saw * 0.6f + noise * 0.15f) * 0.18f;
                }

                s_Creak = AudioClip.Create("Kit Creak", data.Length, 1, k_SampleRate, false);
                s_Creak.SetData(data, 0);
                return s_Creak;
            }
        }

        /// <summary>Plays a clip as a one-shot 3D sound at a world position.</summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume = 0.7f)
        {
            if (clip == null)
                return;

            var go = new GameObject("One Shot Audio");
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.minDistance = 0.5f;
            source.maxDistance = 12f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }

        /// <summary>Plays a clip at the listener (UI sounds).</summary>
        public static void PlayUI(AudioClip clip, float volume = 0.5f)
        {
            var cam = Camera.main;
            PlayAt(clip, cam != null ? cam.transform.position : Vector3.zero, volume);
        }

        static AudioClip Tone(string name, float length, float startHz, float endHz, float decay)
        {
            var data = new float[(int)(k_SampleRate * length)];
            var phase = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var progress = i / (float)data.Length;
                var hz = endHz > 0f ? Mathf.Lerp(startHz, endHz, progress) : startHz;
                phase += 2f * Mathf.PI * hz / k_SampleRate;
                data[i] = Mathf.Sin(phase) * Mathf.Exp(-progress * length * decay) * 0.5f;
            }

            var clip = AudioClip.Create(name, data.Length, 1, k_SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
