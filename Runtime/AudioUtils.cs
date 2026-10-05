using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    internal static class AudioUtils
    {
        internal const float MAX_REVERB_DEPTH = 127f;

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
                SetParameter(mixer, busName, db);
            }
        }

        internal static void SetParameter(AudioMixer mixer, string parameter, float value)
        {
            if (mixer.SetFloat(parameter, value))
            {
                return;
            }

            Debug.LogError($"{nameof(AudioUtils)}::{nameof(SetParameter)} Parameter [{parameter}] is not exposed on mixer [{mixer.name}]. Expose it in the mixer for this setting to take effect");
        }

        /// <summary>
        /// A reverb volume, 0 to 1, as PSX Reverb's Depth, 0 to 127. Linear, as the console's depth register is.
        /// </summary>
        internal static float ReverbVolumeToDepth(float volume)
        {
            float depth = math.clamp(volume, 0f, 1f) * MAX_REVERB_DEPTH;

            return depth;
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