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
        
        private Button PlayMusicButton;
        private Button PlayMusicMutedButton;
        private Button TransitionButton;
        private Button TransitionAllStemsButton;
        private Button PauseMusicButton;
        private Button StopMusicButton;
        private Button StopMusicWithFadeButton;

        private void Awake()
        {
            PlayMusicButton          = m_UIDocument.rootVisualElement.Q<Button>(nameof(PlayMusicButton));
            PlayMusicMutedButton     = m_UIDocument.rootVisualElement.Q<Button>(nameof(PlayMusicMutedButton));
            TransitionButton         = m_UIDocument.rootVisualElement.Q<Button>(nameof(TransitionButton));
            TransitionAllStemsButton = m_UIDocument.rootVisualElement.Q<Button>(nameof(TransitionAllStemsButton));
            PauseMusicButton         = m_UIDocument.rootVisualElement.Q<Button>(nameof(PauseMusicButton));
            StopMusicButton          = m_UIDocument.rootVisualElement.Q<Button>(nameof(StopMusicButton));
            StopMusicWithFadeButton  = m_UIDocument.rootVisualElement.Q<Button>(nameof(StopMusicWithFadeButton));
            
            TransitionButton.SetEnabled(false);
            TransitionAllStemsButton.SetEnabled(false);
            PauseMusicButton.SetEnabled(false);
            StopMusicButton.SetEnabled(false);
            StopMusicWithFadeButton.SetEnabled(false);
        }

        private void Start()
        {
            m_MusicPlayer = new UnityMusicPlayer();
            m_MusicPlayer.SetMusicAssetProvider(m_MusicAssetProvider);
            m_MusicPlayer.SetStemMixerGroups(m_MusicMixerGroups);

            PlayMusicButton.clicked          += OnPlayMusicButton;
            PlayMusicMutedButton.clicked     += OnPlayMusicMutedButton;
            TransitionButton.clicked         += OnTransitionButton;
            TransitionAllStemsButton.clicked += OnTransitionAllStemsButton;
            PauseMusicButton.clicked         += OnPauseMusicButton;
            StopMusicButton.clicked          += OnStopMusicButton;
            StopMusicWithFadeButton.clicked  += OnStopMusicWithFadeButton;
        }

        private void OnDestroy()
        {
            PlayMusicButton.clicked          -= OnPlayMusicButton;
            PlayMusicMutedButton.clicked     -= OnPlayMusicMutedButton;
            TransitionButton.clicked         -= OnTransitionButton;
            TransitionAllStemsButton.clicked -= OnTransitionAllStemsButton;
            PauseMusicButton.clicked         -= OnPauseMusicButton;
            StopMusicButton.clicked          -= OnStopMusicButton;
            StopMusicWithFadeButton.clicked  -= OnStopMusicWithFadeButton;
        }

        private void OnPlayMusicButton()
        {
            m_MusicPlayer.Play(0);
            
            PlayMusicButton.SetEnabled(false);
            PlayMusicMutedButton.SetEnabled(false);
            TransitionButton.SetEnabled(true);
            TransitionAllStemsButton.SetEnabled(true);
            PauseMusicButton.SetEnabled(true);
            StopMusicButton.SetEnabled(true);
            StopMusicWithFadeButton.SetEnabled(true);
        }
        
        private void OnPlayMusicMutedButton()
        {
            m_MusicPlayer.Play(0);
            m_MusicPlayer.SetActiveStemsImmediate(new Dictionary<int, bool>
                                                  {
                                                          { 0, false },
                                                          { 1, true },
                                                          { 2, true },
                                                          { 3, true }
                                                  });
            
            PlayMusicButton.SetEnabled(false);
            PlayMusicMutedButton.SetEnabled(false);
            TransitionButton.SetEnabled(true);
            TransitionAllStemsButton.SetEnabled(true);
            PauseMusicButton.SetEnabled(true);
            StopMusicButton.SetEnabled(true);
            StopMusicWithFadeButton.SetEnabled(true);
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
            
            PlayMusicButton.SetEnabled(true);
            PlayMusicMutedButton.SetEnabled(true);
            TransitionButton.SetEnabled(false);
            TransitionAllStemsButton.SetEnabled(false);
            PauseMusicButton.SetEnabled(false);
            StopMusicButton.SetEnabled(false);
            StopMusicWithFadeButton.SetEnabled(false);
        }
        
        private void OnStopMusicButton()
        {
            m_MusicPlayer.Stop();
            
            PlayMusicButton.SetEnabled(true);
            PlayMusicMutedButton.SetEnabled(true);
            TransitionButton.SetEnabled(false);
            TransitionAllStemsButton.SetEnabled(false);
            PauseMusicButton.SetEnabled(false);
            StopMusicButton.SetEnabled(false);
            StopMusicWithFadeButton.SetEnabled(false);
        }
        
        private void OnStopMusicWithFadeButton()
        {
            m_MusicPlayer.Stop(2f);
            
            PlayMusicButton.SetEnabled(false);
            PlayMusicMutedButton.SetEnabled(false);
            TransitionButton.SetEnabled(false);
            TransitionAllStemsButton.SetEnabled(false);
            PauseMusicButton.SetEnabled(false);
            StopMusicButton.SetEnabled(false);
            StopMusicWithFadeButton.SetEnabled(false);

            _ = Run();

            async Task Run()
            {
                await Awaitable.WaitForSecondsAsync(2f);
                
                PlayMusicButton.SetEnabled(true);
                PlayMusicMutedButton.SetEnabled(true);
            }
        }
    }
}