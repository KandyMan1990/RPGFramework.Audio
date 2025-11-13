using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio.Music;
using RPGFramework.Core;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Music_Sample
{
    public class MusicSample : MonoBehaviour
    {
        [SerializeField]
        private MusicAssetProvider m_MusicAssetProvider;
        [SerializeField]
        private AudioMixerGroup[] m_MusicMixerGroups;
        [SerializeField]
        private UIDocument m_UIDocument;

        private IMusicPlayer m_MusicPlayer;

        private Button m_PlayMusicButton;
        private Button m_PlayMusicMutedButton;
        private Button m_TransitionButton;
        private Button m_TransitionAllStemsButton;
        private Button m_PauseMusicButton;
        private Button m_StopMusicButton;
        private Button m_StopMusicWithFadeButton;
        private Slider m_MusicVolumeSlider;

        private void Awake()
        {
            m_PlayMusicButton          = m_UIDocument.rootVisualElement.Q<Button>("PlayMusicButton");
            m_PlayMusicMutedButton     = m_UIDocument.rootVisualElement.Q<Button>("PlayMusicMutedButton");
            m_TransitionButton         = m_UIDocument.rootVisualElement.Q<Button>("TransitionButton");
            m_TransitionAllStemsButton = m_UIDocument.rootVisualElement.Q<Button>("TransitionAllStemsButton");
            m_PauseMusicButton         = m_UIDocument.rootVisualElement.Q<Button>("PauseMusicButton");
            m_StopMusicButton          = m_UIDocument.rootVisualElement.Q<Button>("StopMusicButton");
            m_StopMusicWithFadeButton  = m_UIDocument.rootVisualElement.Q<Button>("StopMusicWithFadeButton");
            m_MusicVolumeSlider        = m_UIDocument.rootVisualElement.Q<Slider>("MusicVolumeSlider");

            m_TransitionButton.SetEnabled(false);
            m_TransitionAllStemsButton.SetEnabled(false);
            m_PauseMusicButton.SetEnabled(false);
            m_StopMusicButton.SetEnabled(false);
            m_StopMusicWithFadeButton.SetEnabled(false);
        }

        private void Start()
        {
            m_MusicPlayer = new UnityMusicPlayer();
            m_MusicPlayer.SetMusicAssetProvider(m_MusicAssetProvider);
            m_MusicPlayer.SetStemMixerGroups(m_MusicMixerGroups);

            float volume = m_MusicPlayer.GetVolume();
            m_MusicVolumeSlider.SetValueWithoutNotify(volume);

            m_PlayMusicButton.clicked          += OnPlayMusicButton;
            m_PlayMusicMutedButton.clicked     += OnPlayMusicMutedButton;
            m_TransitionButton.clicked         += OnTransitionButton;
            m_TransitionAllStemsButton.clicked += OnTransitionAllStemsButton;
            m_PauseMusicButton.clicked         += OnPauseMusicButton;
            m_StopMusicButton.clicked          += OnStopMusicButton;
            m_StopMusicWithFadeButton.clicked  += OnStopMusicWithFadeButton;

            m_MusicVolumeSlider.RegisterValueChangedCallback(OnVolumeSliderValueChanged);
        }

        private void OnDestroy()
        {
            m_MusicVolumeSlider.UnregisterValueChangedCallback(OnVolumeSliderValueChanged);

            m_StopMusicWithFadeButton.clicked  -= OnStopMusicWithFadeButton;
            m_StopMusicButton.clicked          -= OnStopMusicButton;
            m_PauseMusicButton.clicked         -= OnPauseMusicButton;
            m_TransitionAllStemsButton.clicked -= OnTransitionAllStemsButton;
            m_TransitionButton.clicked         -= OnTransitionButton;
            m_PlayMusicMutedButton.clicked     -= OnPlayMusicMutedButton;
            m_PlayMusicButton.clicked          -= OnPlayMusicButton;
        }

        private void OnPlayMusicButton()
        {
            int musicId = 0;

            // can be awaited if necessary, or can fire and forget like below
            // fire and forget ensure any exceptions are caught and logged correctly
            m_MusicPlayer.Play(musicId).FireAndForget();

            m_PlayMusicButton.SetEnabled(false);
            m_PlayMusicMutedButton.SetEnabled(false);
            m_TransitionButton.SetEnabled(true);
            m_TransitionAllStemsButton.SetEnabled(true);
            m_PauseMusicButton.SetEnabled(true);
            m_StopMusicButton.SetEnabled(true);
            m_StopMusicWithFadeButton.SetEnabled(true);
        }

        private void OnPlayMusicMutedButton()
        {
            m_PlayMusicButton.SetEnabled(false);
            m_PlayMusicMutedButton.SetEnabled(false);
            m_TransitionButton.SetEnabled(true);
            m_TransitionAllStemsButton.SetEnabled(true);
            m_PauseMusicButton.SetEnabled(true);
            m_StopMusicButton.SetEnabled(true);
            m_StopMusicWithFadeButton.SetEnabled(true);

            // async void is bad, but button callbacks have to be void
            // We don't want to await so we call fire and forget to ensure any exceptions get captured
            Run().FireAndForget();

            async Task Run()
            {
                int musicId = 0;

                await m_MusicPlayer.Play(musicId);
                // await Play first as it will set all clip volumes to 1
                m_MusicPlayer.SetActiveStemsImmediate(new Dictionary<int, bool>
                                                      {
                                                              { 0, false },
                                                              { 1, true },
                                                              { 2, true },
                                                              { 3, true }
                                                      });
            }
        }

        private void OnTransitionButton()
        {
            float transitionLength = 2f;
            Dictionary<int, bool> newActiveStems = new Dictionary<int, bool>
                                                   {
                                                           { 0, true },
                                                           { 1, false },
                                                           { 2, true },
                                                           { 3, true }
                                                   };

            // can be awaited if necessary, or can fire and forget like below
            // fire and forget ensure any exceptions are caught and logged correctly
            m_MusicPlayer.SetActiveStemsFade(newActiveStems, transitionLength).FireAndForget();
        }

        private void OnTransitionAllStemsButton()
        {
            float transitionLength = 2f;
            Dictionary<int, bool> newActiveStems = new Dictionary<int, bool>
                                                   {
                                                           { 0, true },
                                                           { 1, true },
                                                           { 2, true },
                                                           { 3, true }
                                                   };

            // can be awaited if necessary, or can fire and forget like below
            // fire and forget ensure any exceptions are caught and logged correctly
            m_MusicPlayer.SetActiveStemsFade(newActiveStems, transitionLength).FireAndForget();
        }

        private void OnPauseMusicButton()
        {
            m_MusicPlayer.Pause();

            m_PlayMusicButton.SetEnabled(true);
            m_PlayMusicMutedButton.SetEnabled(true);
            m_TransitionButton.SetEnabled(false);
            m_TransitionAllStemsButton.SetEnabled(false);
            m_PauseMusicButton.SetEnabled(false);
            m_StopMusicButton.SetEnabled(false);
            m_StopMusicWithFadeButton.SetEnabled(false);
        }

        private void OnStopMusicButton()
        {
            // can be awaited if necessary, or can fire and forget like below
            // fire and forget ensure any exceptions are caught and logged correctly
            m_MusicPlayer.Stop().FireAndForget();

            m_PlayMusicButton.SetEnabled(true);
            m_PlayMusicMutedButton.SetEnabled(true);
            m_TransitionButton.SetEnabled(false);
            m_TransitionAllStemsButton.SetEnabled(false);
            m_PauseMusicButton.SetEnabled(false);
            m_StopMusicButton.SetEnabled(false);
            m_StopMusicWithFadeButton.SetEnabled(false);
        }

        private void OnStopMusicWithFadeButton()
        {
            m_PlayMusicButton.SetEnabled(false);
            m_PlayMusicMutedButton.SetEnabled(false);
            m_TransitionButton.SetEnabled(false);
            m_TransitionAllStemsButton.SetEnabled(false);
            m_PauseMusicButton.SetEnabled(false);
            m_StopMusicButton.SetEnabled(false);
            m_StopMusicWithFadeButton.SetEnabled(false);

            // async void is bad, but button callbacks have to be void
            // We don't want to await so we call fire and forget to ensure any exceptions get captured
            Run().FireAndForget();

            async Task Run()
            {
                await m_MusicPlayer.Stop(2f);

                m_PlayMusicButton.SetEnabled(true);
                m_PlayMusicMutedButton.SetEnabled(true);
            }
        }

        private void OnVolumeSliderValueChanged(ChangeEvent<float> value)
        {
            m_MusicPlayer.SetVolume(value.newValue);
        }
    }
}