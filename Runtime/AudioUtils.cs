using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    internal static class AudioUtils
    {
        private const float MIN_DB              = -80f;
        private const float PERCEPTUAL_EXPONENT = 1.661f;

        internal static float GetVolume(AudioMixer mixer, string busName)
        {
            if (!mixer.GetFloat(busName, out float db))
            {
                Debug.LogError($"{nameof(AudioUtils)}::{nameof(GetVolume)} Parameter [{busName}] is not exposed on mixer [{mixer.name}]. Expose it in the mixer to read this volume");

                return 0f;
            }

            float percent = DbToPercent(db);

            return percent;
        }

        internal static void SetVolume(AudioMixer mixer, string[] busNames, float percent)
        {
            float db = PercentToDb(percent);

            foreach (string busName in busNames)
            {
                if (mixer.SetFloat(busName, db))
                {
                    continue;
                }

                Debug.LogError($"{nameof(AudioUtils)}::{nameof(SetVolume)} Parameter [{busName}] is not exposed on mixer [{mixer.name}]. Expose it in the mixer for this volume to take effect");
            }
        }

        internal static float DbToPercent(float db)
        {
            if (db <= MIN_DB + 0.01f)
            {
                return 0f;
            }

            float amplitude = math.pow(10f,       db / 20f);
            float result    = math.pow(amplitude, 1f / PERCEPTUAL_EXPONENT);

            return result;
        }

        internal static float PercentToDb(float percent)
        {
            float clamp = math.clamp(percent, 0f, 1f);

            if (clamp <= 0f)
            {
                return MIN_DB;
            }

            float amplitude = math.pow(clamp, PERCEPTUAL_EXPONENT);

            float result = math.max(20f * math.log10(amplitude), MIN_DB);

            return result;
        }
    }
}