using System.Threading.Tasks;
using RPGFramework.Audio.Music;
using RPGFramework.Hashing;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Music_Sample
{
    public class MusicSample : MonoBehaviour
    {
        // The stem states the music asset declares, in the order they are authored on it. Which stems
        // each one turns on is decided on the asset, so this sample only has to know what it wants to
        // hear, not which track is which.
        // Tracks and stem states are both named, and the name is hashed, so that reordering either list
        // cannot repoint a script or a caller. The names are the asset's own.
        private const string TRACK = "Music Sample";

        private const string DEFAULT                   = "Default";
        private const string STATE_WITHOUT_FIRST_STEM  = "WithoutFirstStem";
        private const string STATE_WITHOUT_SECOND_STEM = "WithoutSecondStem";

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
            // can be awaited if necessary, or can fire and forget like below
            // fire and forget ensure any exceptions are caught and logged correctly
            m_MusicPlayer.PlayAsync(Fnv1a64.Hash(TRACK)).FireAndForget();

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
                // sets the first stems volume to 0
                // handy if you want to crossfade stems at some point
                // this keeps the tracks aligned
                await m_MusicPlayer.PlayAsync(Fnv1a64.Hash(TRACK), Fnv1a64.Hash(STATE_WITHOUT_FIRST_STEM));
            }
        }

        private void OnTransitionButton()
        {
            float transitionLength = 2f;

            // can be awaited if necessary, or can fire and forget like below
            // fire and forget ensure any exceptions are caught and logged correctly
            m_MusicPlayer.SetStemStateFadeAsync(Fnv1a64.Hash(STATE_WITHOUT_SECOND_STEM), transitionLength).FireAndForget();
        }

        private void OnTransitionAllStemsButton()
        {
            float transitionLength = 2f;

            // can be awaited if necessary, or can fire and forget like below
            // fire and forget ensure any exceptions are caught and logged correctly
            m_MusicPlayer.SetStemStateFadeAsync(Fnv1a64.Hash(DEFAULT), transitionLength).FireAndForget();
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
            m_MusicPlayer.StopAsync().FireAndForget();

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
                await m_MusicPlayer.StopAsync(2f);

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