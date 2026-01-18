using UnityEngine;

namespace Uniforge.FastTrack.Runtime
{
    public class AudioManager : MonoBehaviour
    {
        public void PlaySFX(string sfxName, float volume = 1f)
        {
            Debug.Log($"[AudioManager] Play SFX: {sfxName}, Volume: {volume}");
        }

        public void PlayBGM(string bgmName, float volume = 1f)
        {
            Debug.Log($"[AudioManager] Play BGM: {bgmName}, Volume: {volume}");
        }

        public void StopBGM()
        {
            Debug.Log("[AudioManager] Stop BGM");
        }

        public static void PlaySFXStatic(string sfxName, float volume = 1f)
        {
            if (UniforgeRuntime.Instance?.Audio != null)
                UniforgeRuntime.Instance.Audio.PlaySFX(sfxName, volume);
        }

        public static void PlayBGMStatic(string bgmName, float volume = 1f)
        {
            if (UniforgeRuntime.Instance?.Audio != null)
                UniforgeRuntime.Instance.Audio.PlayBGM(bgmName, volume);
        }
    }
}
