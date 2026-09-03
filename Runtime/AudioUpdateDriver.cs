using UnityEngine;

namespace RPGFramework.Audio
{
    internal sealed class AudioUpdateDriver : MonoBehaviour
    {
        private IAudioUpdatable m_Target;

        internal static AudioUpdateDriver Attach(GameObject playerObject, IAudioUpdatable target)
        {
            AudioUpdateDriver driver = playerObject.AddComponent<AudioUpdateDriver>();
            driver.m_Target = target;

            return driver;
        }

        private void Update()
        {
            m_Target.Update();
        }
    }
}