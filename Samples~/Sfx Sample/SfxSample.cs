using System.Collections.Generic;
using System.Threading.Tasks;
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

        private Button m_PlaySfx0Button;
        private Button m_PlaySfx0ButtonWithReference;
        // private Button m_TransitionButton;
        // private Button m_TransitionAllStemsButton;
        // private Button m_PauseMusicButton;
        // private Button m_StopMusicButton;
        // private Button m_StopMusicWithFadeButton;

        private void Awake()
        {
            m_PlaySfx0Button              = m_UIDocument.rootVisualElement.Q<Button>("PlaySfx0Button");
            m_PlaySfx0ButtonWithReference = m_UIDocument.rootVisualElement.Q<Button>("PlaySfx0ButtonWithReference");
            // m_TransitionButton         = m_UIDocument.rootVisualElement.Q<Button>("TransitionButton");
            // m_TransitionAllStemsButton = m_UIDocument.rootVisualElement.Q<Button>("TransitionAllStemsButton");
            // m_PauseMusicButton         = m_UIDocument.rootVisualElement.Q<Button>("PauseMusicButton");
            // m_StopMusicButton          = m_UIDocument.rootVisualElement.Q<Button>("StopMusicButton");
            // m_StopMusicWithFadeButton  = m_UIDocument.rootVisualElement.Q<Button>("StopMusicWithFadeButton");

            // m_TransitionButton.SetEnabled(false);
            // m_TransitionAllStemsButton.SetEnabled(false);
            // m_PauseMusicButton.SetEnabled(false);
            // m_StopMusicButton.SetEnabled(false);
            // m_StopMusicWithFadeButton.SetEnabled(false);
        }

        private void Start()
        {
            m_SfxPlayer = new UnitySfxPlayer();
            m_SfxPlayer.SetSfxAssetProvider(m_SfxAssetProvider);
            m_SfxPlayer.SetStemMixerGroups(m_SfxMixerGroups);

            m_PlaySfx0Button.clicked              += OnPlaySfx0Button;
            m_PlaySfx0ButtonWithReference.clicked += OnPlaySfx0ButtonWithReference;
            // m_TransitionButton.clicked         += OnTransitionButton;
            // m_TransitionAllStemsButton.clicked += OnTransitionAllStemsButton;
            // m_PauseMusicButton.clicked         += OnPauseMusicButton;
            // m_StopMusicButton.clicked          += OnStopMusicButton;
            // m_StopMusicWithFadeButton.clicked  += OnStopMusicWithFadeButton;
        }

        private void OnDestroy()
        {
            m_PlaySfx0Button.clicked              -= OnPlaySfx0Button;
            m_PlaySfx0ButtonWithReference.clicked -= OnPlaySfx0ButtonWithReference;
            // m_TransitionButton.clicked         -= OnTransitionButton;
            // m_TransitionAllStemsButton.clicked -= OnTransitionAllStemsButton;
            // m_PauseMusicButton.clicked         -= OnPauseMusicButton;
            // m_StopMusicButton.clicked          -= OnStopMusicButton;
            // m_StopMusicWithFadeButton.clicked  -= OnStopMusicWithFadeButton;
        }

        private void OnPlaySfx0Button()
        {
            // trigger a sound
            m_SfxPlayer.Play(0);
        }

        private void OnPlaySfx0ButtonWithReference()
        {
            // trigger a sound
            ISfxReference sfxReference = m_SfxPlayer.Play(0);

            sfxReference.OnEvent += SfxReferenceOnEvent;

            void SfxReferenceOnEvent(string eventName, ISfxReference sfxRef)
            {
                Debug.Log($"{eventName} event triggered");
            }
        }

        // private void OnPlayMusicMutedButton()
        // {
        //     // await if needed
        //     m_MusicPlayer.Play(0);
        //     m_MusicPlayer.SetActiveStemsImmediate(new Dictionary<int, bool>
        //                                           {
        //                                                   { 0, false },
        //                                                   { 1, true },
        //                                                   { 2, true },
        //                                                   { 3, true }
        //                                           });
        //
        //     m_PlaySfx0Button.SetEnabled(false);
        //     m_PlaySfx0ButtonWithReference.SetEnabled(false);
        //     m_TransitionButton.SetEnabled(true);
        //     m_TransitionAllStemsButton.SetEnabled(true);
        //     m_PauseMusicButton.SetEnabled(true);
        //     m_StopMusicButton.SetEnabled(true);
        //     m_StopMusicWithFadeButton.SetEnabled(true);
        // }
        //
        // private void OnTransitionButton()
        // {
        //     m_MusicPlayer.SetActiveStemsFade(new Dictionary<int, bool>
        //                                      {
        //                                              { 0, true },
        //                                              { 1, false },
        //                                              { 2, true },
        //                                              { 3, true }
        //                                      },
        //                                      2f);
        // }
        //
        // private void OnTransitionAllStemsButton()
        // {
        //     m_MusicPlayer.SetActiveStemsFade(new Dictionary<int, bool>
        //                                      {
        //                                              { 0, true },
        //                                              { 1, true },
        //                                              { 2, true },
        //                                              { 3, true }
        //                                      },
        //                                      2f);
        // }
        //
        // private void OnPauseMusicButton()
        // {
        //     m_MusicPlayer.Pause();
        //
        //     m_PlaySfx0Button.SetEnabled(true);
        //     m_PlaySfx0ButtonWithReference.SetEnabled(true);
        //     m_TransitionButton.SetEnabled(false);
        //     m_TransitionAllStemsButton.SetEnabled(false);
        //     m_PauseMusicButton.SetEnabled(false);
        //     m_StopMusicButton.SetEnabled(false);
        //     m_StopMusicWithFadeButton.SetEnabled(false);
        // }
        //
        // private void OnStopMusicButton()
        // {
        //     // await if needed
        //     m_MusicPlayer.Stop();
        //
        //     m_PlaySfx0Button.SetEnabled(true);
        //     m_PlaySfx0ButtonWithReference.SetEnabled(true);
        //     m_TransitionButton.SetEnabled(false);
        //     m_TransitionAllStemsButton.SetEnabled(false);
        //     m_PauseMusicButton.SetEnabled(false);
        //     m_StopMusicButton.SetEnabled(false);
        //     m_StopMusicWithFadeButton.SetEnabled(false);
        // }
        //
        // private void OnStopMusicWithFadeButton()
        // {
        //     m_PlaySfx0Button.SetEnabled(false);
        //     m_PlaySfx0ButtonWithReference.SetEnabled(false);
        //     m_TransitionButton.SetEnabled(false);
        //     m_TransitionAllStemsButton.SetEnabled(false);
        //     m_PauseMusicButton.SetEnabled(false);
        //     m_StopMusicButton.SetEnabled(false);
        //     m_StopMusicWithFadeButton.SetEnabled(false);
        //
        //     // async void bad, but button callbacks have to be void
        //     // _ = means "I don't want to await this task"
        //     _ = Run();
        //
        //     async Task Run()
        //     {
        //         await m_MusicPlayer.Stop(2f);
        //
        //         m_PlaySfx0Button.SetEnabled(true);
        //         m_PlaySfx0ButtonWithReference.SetEnabled(true);
        //     }
        // }
    }
}