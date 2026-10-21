using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PingHeim
{
    /// <summary>The ping sound, loaded from the file beside the plugin.</summary>
    internal static class PingSound
    {
        private static AudioClip _clip;
        private static AudioSource _source;

        public static IEnumerator Load(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                PingHeimPlugin.Log.LogWarning($"The ping sound isn't there: {path}");
                yield break;
            }

            string url = new System.Uri(path).AbsoluteUri;
            using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    PingHeimPlugin.Log.LogWarning($"Couldn't load the ping sound: {request.error}");
                    yield break;
                }

                _clip = DownloadHandlerAudioClip.GetContent(request);
                _clip.name = "PingHeimPing";
            }
        }

        /// <summary>Plays the ping to the local player.</summary>
        public static void Play()
        {
            if (_clip == null) return;

            if (_source == null)
            {
                var go = new GameObject("PingHeimAudio");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.spatialBlend = 0f;
                _source.playOnAwake = false;
                _source.ignoreListenerPause = true;
            }

            // The game's interface sound group, so the player's volume settings apply.
            if (AudioMan.instance != null) _source.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
            _source.PlayOneShot(_clip, Mathf.Clamp01(PingHeimPlugin.Volume.Value));
        }
    }
}
