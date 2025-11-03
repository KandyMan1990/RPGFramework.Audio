using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    internal interface IStem
    {
        internal AudioClip Clip            { get; }
        internal float     ReverbSendLevel { get; }
    }

    internal interface IMusicAsset
    {
        internal double               LoopStartTime { get; }
        internal double               LoopEndTime   { get; }
        internal bool                 Loop          { get; }
        internal IReadOnlyList<IStem> Tracks        { get; }
        internal void                 CalculateLoopPoints();
    }

    public interface IMusicAssetProvider
    {
        internal IMusicAsset GetMusicAsset(int id);
    }

    public interface IMusicPlayer
    {
        Task Play(int id);
        void Pause();
        Task Stop(float fadeTime = 0.001f);
        void ClearPausedMusic();
        void SetMusicAssetProvider(IMusicAssetProvider     provider);
        void SetStemMixerGroups(AudioMixerGroup[]          groups);
        Task SetActiveStemsFade(Dictionary<int, bool>      stemValues, float transitionLength);
        void SetActiveStemsImmediate(Dictionary<int, bool> stemValues);
    }

    public interface ISfxEventData
    {
        string        EventName                 { get; }
        float         EventTriggerTime          { get; }
        internal int  EventTriggerTimeInSamples { get; }
        internal bool RemoveEventOnceTriggered  { get; }
        void          SetSampleRate(int sampleRate);
    }

    internal interface ISfxAsset
    {
        internal IReadOnlyList<IStem>         Tracks    { get; }
        internal IReadOnlyList<ISfxEventData> Events    { get; }
        internal bool                         Loop      { get; }
        internal int                          LoopStart { get; }
        internal int                          LoopEnd   { get; }
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
        internal ISfxAsset GetSfxAsset(int id);
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
        void          Dispose();
    }
}