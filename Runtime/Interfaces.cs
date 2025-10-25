using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    public interface IStem
    {
        public AudioClip Clip            { get; }
        public float     ReverbSendLevel { get; }
    }

    public interface IMusicAsset
    {
        double               LoopStartTime { get; }
        double               LoopEndTime   { get; }
        bool                 Loop          { get; }
        IReadOnlyList<IStem> Tracks        { get; }
        void                 CalculateLoopPoints();
    }

    public interface IMusicAssetProvider
    {
        IMusicAsset GetMusicAsset(int id);
    }

    public interface IMusicPlayer
    {
        Task Play(int id);
        void Pause();
        Task Stop(float fadeTime = 0f);
        void ClearPausedMusic();
        void SetMusicAssetProvider(IMusicAssetProvider     provider);
        void SetStemMixerGroups(AudioMixerGroup[]          groups);
        void SetActiveStemsFade(Dictionary<int, bool>      stemValues, float transitionLength);
        void SetActiveStemsImmediate(Dictionary<int, bool> stemValues);
    }

    public interface ISfxEventData
    {
        string EventName        { get; }
        float  EventTriggerTime { get; }
    }

    public interface ISfxAsset
    {
        IReadOnlyList<IStem>         Tracks    { get; }
        IReadOnlyList<ISfxEventData> Events    { get; }
        bool                         Loop      { get; }
        int                          LoopStart { get; }
        int                          LoopEnd   { get; }
    }

    public interface ISfxReference
    {
        event Action<string, ISfxReference> OnEvent;
        IReadOnlyList<ISfxEventData>        Events { get; }
        internal void                       CheckForLoop();
        internal void                       CheckForEventToRaise();
        internal void                       Pause();
        internal void                       Resume();
        internal void                       Stop();
    }

    public interface ISfxAssetProvider
    {
        ISfxAsset GetSfxAsset(int id);
    }

    public interface ISfxPlayer
    {
        ISfxReference Play(int            id);
        void          Pause(ISfxReference sfxReference);
        void          PauseAll();
        void          Resume(ISfxReference sfxReference);
        void          ResumeAll();
        void          Stop(ISfxReference sfxReference);
        void          StopAll();
        void          SetSfxAssetProvider(ISfxAssetProvider provider);
        void          SetStemMixerGroups(AudioMixerGroup[]  groups);
    }
}