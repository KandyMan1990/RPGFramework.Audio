using RPGFramework.Audio.Sfx;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Sfx_Sample
{
    public class SfxSample : MonoBehaviour
    {
        [SerializeField]
        private SfxAssetProvider m_SfxAssetProvider;
        [SerializeField]
        private AudioMixerGroup[] m_SfxMixerGroups;
        [SerializeField]
        private UIDocument m_UIDocument;

        private ISfxPlayer m_SfxPlayer;

        private Button m_PlaySfx1Button;
        private Button m_StopSfx1Button;
        private Button m_PlaySfx0ButtonWithLoopAndEvent;
        private Button m_PlayAmbienceButton;
        private Button m_StopAllSfxButton;
        private Slider m_SfxVolumeSlider;

        private ISfxReference m_SfxReference0;

        private void Awake()
        {
            m_PlaySfx1Button                 = m_UIDocument.rootVisualElement.Q<Button>("PlaySfx1Button");
            m_StopSfx1Button                 = m_UIDocument.rootVisualElement.Q<Button>("StopSfx1Button");
            m_PlaySfx0ButtonWithLoopAndEvent = m_UIDocument.rootVisualElement.Q<Button>("PlaySfx0ButtonWithLoopAndEvent");
            m_PlayAmbienceButton             = m_UIDocument.rootVisualElement.Q<Button>("PlayAmbienceButton");
            m_StopAllSfxButton               = m_UIDocument.rootVisualElement.Q<Button>("StopAllSfxButton");
            m_SfxVolumeSlider                = m_UIDocument.rootVisualElement.Q<Slider>("SfxVolumeSlider");
        }

        private void Start()
        {
            m_SfxPlayer = new UnitySfxPlayer();
            m_SfxPlayer.SetSfxAssetProvider(m_SfxAssetProvider);
            m_SfxPlayer.SetStemMixerGroups(m_SfxMixerGroups);

            float volume = m_SfxPlayer.GetVolume();
            m_SfxVolumeSlider.SetValueWithoutNotify(volume);

            m_PlaySfx1Button.clicked                 += OnPlaySfx1Button;
            m_StopSfx1Button.clicked                 += OnStopSfx1Button;
            m_PlaySfx0ButtonWithLoopAndEvent.clicked += OnPlaySfx0ButtonWithLoopAndEvent;
            m_PlayAmbienceButton.clicked             += OnPlayAmbienceButton;
            m_StopAllSfxButton.clicked               += OnStopAllSfxButton;

            m_SfxVolumeSlider.RegisterValueChangedCallback(OnVolumeSliderValueChanged);
        }

        private void OnDestroy()
        {
            m_SfxVolumeSlider.UnregisterValueChangedCallback(OnVolumeSliderValueChanged);

            m_StopAllSfxButton.clicked               -= OnStopAllSfxButton;
            m_PlayAmbienceButton.clicked             -= OnPlayAmbienceButton;
            m_PlaySfx0ButtonWithLoopAndEvent.clicked -= OnPlaySfx0ButtonWithLoopAndEvent;
            m_StopSfx1Button.clicked                 -= OnStopSfx1Button;
            m_PlaySfx1Button.clicked                 -= OnPlaySfx1Button;
        }

        private static void SfxReferenceOnEvent(string eventName, ISfxReference sfxRef)
        {
            Debug.Log($"{eventName} event triggered");
        }

        private void OnPlaySfx1Button()
        {
            // trigger a sound
            m_SfxReference0 = m_SfxPlayer.Play(1);
        }

        private void OnStopSfx1Button()
        {
            // stop a sound
            m_SfxPlayer.Stop(m_SfxReference0);
        }

        private void OnPlaySfx0ButtonWithLoopAndEvent()
        {
            // trigger a sound
            ISfxReference sfxReference = m_SfxPlayer.Play(0);

            // see when each event will be triggered in seconds
            foreach (ISfxEventData sfxEventData in sfxReference.Events)
            {
                Debug.Log($"{sfxEventData.EventName} {sfxEventData.EventTriggerTime}");
            }

            sfxReference.OnEvent += SfxReferenceOnEvent;
        }

        private void OnPlayAmbienceButton()
        {
            // trigger ambience
            ISfxReference sfxReference = m_SfxReference0 = m_SfxPlayer.Play(2);

            sfxReference.OnEvent += SfxReferenceOnEvent;
        }

        private void OnStopAllSfxButton()
        {
            m_SfxPlayer.StopAll();
        }

        private void OnVolumeSliderValueChanged(ChangeEvent<float> value)
        {
            m_SfxPlayer.SetVolume(value.newValue);
        }
    }
}