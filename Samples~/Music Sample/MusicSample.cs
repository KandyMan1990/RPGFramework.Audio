using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio.Music;
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

        private void Awake()
        {
            m_PlayMusicButton          = m_UIDocument.rootVisualElement.Q<Button>("PlayMusicButton");
            m_PlayMusicMutedButton     = m_UIDocument.rootVisualElement.Q<Button>("PlayMusicMutedButton");
            m_TransitionButton         = m_UIDocument.rootVisualElement.Q<Button>("TransitionButton");
            m_TransitionAllStemsButton = m_UIDocument.rootVisualElement.Q<Button>("TransitionAllStemsButton");
            m_PauseMusicButton         = m_UIDocument.rootVisualElement.Q<Button>("PauseMusicButton");
            m_StopMusicButton          = m_UIDocument.rootVisualElement.Q<Button>("StopMusicButton");
            m_StopMusicWithFadeButton  = m_UIDocument.rootVisualElement.Q<Button>("StopMusicWithFadeButton");

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

            m_PlayMusicButton.clicked          += OnPlayMusicButton;
            m_PlayMusicMutedButton.clicked     += OnPlayMusicMutedButton;
            m_TransitionButton.clicked         += OnTransitionButton;
            m_TransitionAllStemsButton.clicked += OnTransitionAllStemsButton;
            m_PauseMusicButton.clicked         += OnPauseMusicButton;
            m_StopMusicButton.clicked          += OnStopMusicButton;
            m_StopMusicWithFadeButton.clicked  += OnStopMusicWithFadeButton;
        }

        private void OnDestroy()
        {
            m_PlayMusicButton.clicked          -= OnPlayMusicButton;
            m_PlayMusicMutedButton.clicked     -= OnPlayMusicMutedButton;
            m_TransitionButton.clicked         -= OnTransitionButton;
            m_TransitionAllStemsButton.clicked -= OnTransitionAllStemsButton;
            m_PauseMusicButton.clicked         -= OnPauseMusicButton;
            m_StopMusicButton.clicked          -= OnStopMusicButton;
            m_StopMusicWithFadeButton.clicked  -= OnStopMusicWithFadeButton;
        }

        private void OnPlayMusicButton()
        {
            // await if needed
            m_MusicPlayer.Play(0);

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
            // await if needed
            m_MusicPlayer.Play(0);
            m_MusicPlayer.SetActiveStemsImmediate(new Dictionary<int, bool>
                                                  {
                                                          { 0, false },
                                                          { 1, true },
                                                          { 2, true },
                                                          { 3, true }
                                                  });

            m_PlayMusicButton.SetEnabled(false);
            m_PlayMusicMutedButton.SetEnabled(false);
            m_TransitionButton.SetEnabled(true);
            m_TransitionAllStemsButton.SetEnabled(true);
            m_PauseMusicButton.SetEnabled(true);
            m_StopMusicButton.SetEnabled(true);
            m_StopMusicWithFadeButton.SetEnabled(true);
        }

        private void OnTransitionButton()
        {
            m_MusicPlayer.SetActiveStemsFade(new Dictionary<int, bool>
                                             {
                                                     { 0, true },
                                                     { 1, false },
                                                     { 2, true },
                                                     { 3, true }
                                             },
                                             2f);
        }

        private void OnTransitionAllStemsButton()
        {
            m_MusicPlayer.SetActiveStemsFade(new Dictionary<int, bool>
                                             {
                                                     { 0, true },
                                                     { 1, true },
                                                     { 2, true },
                                                     { 3, true }
                                             },
                                             2f);
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
            // await if needed
            m_MusicPlayer.Stop();

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

            // async void bad, but button callbacks have to be void
            // _ = means "I don't want to await this task"
            _ = Run();

            async Task Run()
            {
                await m_MusicPlayer.Stop(2f);

                m_PlayMusicButton.SetEnabled(true);
                m_PlayMusicMutedButton.SetEnabled(true);
            }
        }
    }
}