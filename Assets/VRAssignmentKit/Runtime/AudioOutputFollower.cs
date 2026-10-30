using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace VRAssignment
{
    /// <summary>
    /// Makes Unity's audio play through the PC's current output device, so a Quest on Link / Air Link hears it.
    ///
    /// Unity opens the Windows default audio device once and keeps using it. With Quest Link the headset's
    /// device ("Headphones (Oculus Virtual Audio Device)" / "Meta Quest") often becomes the default only after
    /// Link connects, so the TV, sockets, door and UI keep playing through the PC speakers or monitor and the
    /// headset is silent, even though everything "has sound" when testing in the Editor.
    ///
    /// This does two things, by itself, in every scene (nothing needs to be added to the hierarchy):
    ///  1. When Play starts (Editor / Windows build) it re-opens audio on whatever device is the default right now.
    ///  2. If Windows switches the default device while running, it moves the audio over and restarts whatever
    ///     was playing (AudioSources and the TV's VideoPlayer).
    /// </summary>
    public static class AudioOutputFollower
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnConfigurationChanged;
            AudioSettings.OnAudioConfigurationChanged += OnConfigurationChanged;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            // Nothing is playing yet, so this simply re-opens the current default output device.
            AudioSettings.Reset(AudioSettings.GetConfiguration());
#endif
        }

        static void OnConfigurationChanged(bool deviceWasChanged)
        {
            if (!deviceWasChanged)
                return;

            // Remember what was playing, because a reset stops every AudioSource.
            var sources = new List<(AudioSource source, float time)>();
            foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source != null && source.isPlaying && source.clip != null)
                    sources.Add((source, source.time));
            }

            var videos = new List<(VideoPlayer player, double time)>();
            foreach (var player in Object.FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None))
            {
                if (player != null && player.isPlaying)
                    videos.Add((player, player.time));
            }

            AudioSettings.Reset(AudioSettings.GetConfiguration());

            foreach (var (source, time) in sources)
            {
                if (source == null || !source.isActiveAndEnabled)
                    continue;
                source.Play();
                if (time < source.clip.length)
                    source.time = time;
            }

            foreach (var (player, time) in videos)
            {
                if (player == null || !player.isActiveAndEnabled)
                    continue;
                player.Stop();
                player.Play();
                player.time = time;
            }

            Debug.Log("[Audio] Output device changed - Unity audio moved to the new default device.");
        }
    }
}
