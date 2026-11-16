using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Sfx
{
    public sealed class UnitySfxPlayer : ISfxPlayer, IAudioUpdatable
    {
        private const string SFX_BUS_NAME         = "Sfx";
        private const string SFX_REVERB_SEND      = "SfxReverbSend";
        private const string SFX_GAME_OBJECT_NAME = "SfxPlayer";

        private static readonly string[] VOLUME_BUS_NAMES = { SFX_BUS_NAME, SFX_REVERB_SEND };

        private readonly ISfxPlayer                 m_This;
        private readonly List<ISfxReference>        m_SfxReferences;
        private readonly List<ISfxReference>        m_UpdateBuffer;
        private readonly List<ISfxAsset>            m_Loading;
        private readonly AudioPreloads<SfxPlayable> m_Preloads;
        private readonly Dictionary<ulong, int>     m_LastPicked;

        private ISfxAssetProvider m_SfxAssetProvider;
        private AudioChannels     m_Channels;
        private IStemPlayback[]   m_Playbacks;
        private AudioMixer        m_AudioMixer;
        private bool              m_Disposed;
        private ISfxReference[]   m_VoiceOwners;
        private int               m_Stops;
        private SfxLevel          m_AllVolume;

        public UnitySfxPlayer()
        {
            m_SfxReferences = new List<ISfxReference>();
            m_UpdateBuffer  = new List<ISfxReference>();
            m_Loading       = new List<ISfxAsset>();
            m_Preloads      = new AudioPreloads<SfxPlayable>();
            m_LastPicked    = new Dictionary<ulong, int>();
            m_This          = this;
            m_AllVolume     = new SfxLevel(1f);
        }

        Task<ISfxReference> ISfxPlayer.PlayAsync(ulong nameHash)
        {
            return PlayAsync(nameHash, 1f, 0f, 0f, ISfxPlayer.NEXT_IN_ORDER);
        }

        Task<ISfxReference> ISfxPlayer.PlayAsync(ulong nameHash, float volume, float pan, float startSeconds, int position)
        {
            return PlayAsync(nameHash, math.saturate(volume), math.clamp(pan, -1f, 1f), startSeconds, position);
        }

        private async Task<ISfxReference> PlayAsync(ulong nameHash, float volume, float pan, float startSeconds, int position)
        {
            int         stops    = m_Stops;
            SfxPlayable playable = await m_SfxAssetProvider.AcquireAsync(nameHash);
            ISfxAsset   sfxAsset = PickRecording(nameHash, playable, position);
            float       pitch    = SfxVariation.PickPitch(playable.PitchVariation, UnityEngine.Random.value);

            m_Loading.Add(sfxAsset);

            try
            {
                await StemClips.LoadAsync(sfxAsset, () => stops != m_Stops || m_Disposed);
            }
            finally
            {
                m_Loading.Remove(sfxAsset);
            }

            if (stops != m_Stops || m_Disposed)
            {
                StemClips.UnloadUnused(sfxAsset, IsClipInUse);
                m_SfxAssetProvider.Release(nameHash);

                ISfxReference stopped = StoppedReference(nameHash, sfxAsset);

                return stopped;
            }

            ISfxReference playing = ScheduleSfx(nameHash, sfxAsset, startSeconds, volume, pan, pitch);

            return playing;
        }

        Task ISfxPlayer.PreloadAsync(IReadOnlyList<ulong> nameHashes)
        {
            Task[] preloads = new Task[nameHashes.Count];

            for (int i = 0; i < nameHashes.Count; i++)
            {
                preloads[i] = PreloadAsync(nameHashes[i]);
            }

            return Task.WhenAll(preloads);
        }

        void ISfxPlayer.Unload(IReadOnlyList<ulong> nameHashes)
        {
            for (int i = 0; i < nameHashes.Count; i++)
            {
                if (!m_Preloads.TryEnd(nameHashes[i], out SfxPlayable playable))
                {
                    continue;
                }

                for (int j = 0; j < playable.Recordings.Count; j++)
                {
                    StemClips.UnloadUnused(playable.Recordings[j], IsClipInUse);
                }

                m_SfxAssetProvider.Release(nameHashes[i]);
            }
        }

        void ISfxPlayer.SetSoundVolume(ISfxReference sfxReference, float volume, float seconds)
        {
            sfxReference.SetVolume(math.saturate(volume), seconds);
        }

        void ISfxPlayer.SetSoundPan(ISfxReference sfxReference, float pan, float seconds)
        {
            sfxReference.SetPan(math.clamp(pan, -1f, 1f), seconds);
        }

        void ISfxPlayer.SetAllSoundsVolume(float volume, float seconds)
        {
            m_AllVolume.MoveTo(math.saturate(volume), seconds);
        }

        void ISfxPlayer.Pause(ISfxReference sfxReference)
        {
            sfxReference.Pause();
        }

        void ISfxPlayer.PauseAll()
        {
            m_Stops++;

            for (int i = 0; i < m_SfxReferences.Count; i++)
            {
                ISfxReference sfxReference = m_SfxReferences[i];

                m_This.Pause(sfxReference);
            }
        }

        void ISfxPlayer.Resume(ISfxReference sfxReference)
        {
            sfxReference.Resume();
        }

        void ISfxPlayer.ResumeAll()
        {
            for (int i = 0; i < m_SfxReferences.Count; i++)
            {
                ISfxReference sfxReference = m_SfxReferences[i];

                m_This.Resume(sfxReference);
            }
        }

        void ISfxPlayer.Stop(ISfxReference sfxReference)
        {
            if (!m_SfxReferences.Remove(sfxReference))
            {
                return;
            }

            sfxReference.Stop();

            ReleaseReference(sfxReference);
        }

        void ISfxPlayer.StopAll()
        {
            m_Stops++;

            for (int i = m_SfxReferences.Count - 1; i >= 0; i--)
            {
                ISfxReference sfxReference = m_SfxReferences[i];
                m_This.Stop(sfxReference);
            }

            m_SfxReferences.Clear();
        }

        void ISfxPlayer.SetSfxAssetProvider(ISfxAssetProvider provider)
        {
            m_SfxAssetProvider = provider;
        }

        void ISfxPlayer.SetStemMixerGroups(AudioMixerGroup[] groups)
        {
            DestroyChannels();

            m_Channels    = new AudioChannels(SFX_GAME_OBJECT_NAME, groups, this);
            m_Playbacks   = m_Channels.Playbacks;
            m_AudioMixer  = m_Channels.Mixer;
            m_VoiceOwners = new ISfxReference[groups.Length];
        }

        float ISfxPlayer.GetVolume()
        {
            return AudioUtils.GetVolume(m_AudioMixer, SFX_BUS_NAME);
        }

        void ISfxPlayer.SetVolume(float percent)
        {
            AudioUtils.SetVolume(m_AudioMixer, VOLUME_BUS_NAMES, percent);
        }

        void IAudioUpdatable.Update()
        {
            float deltaTime  = Time.unscaledDeltaTime;
            bool  allChanged = m_AllVolume.Tick(deltaTime);

            if (m_SfxReferences.Count == 0)
            {
                return;
            }

            m_UpdateBuffer.Clear();
            m_UpdateBuffer.AddRange(m_SfxReferences);

            for (int i = 0; i < m_UpdateBuffer.Count; i++)
            {
                ISfxReference sfxReference = m_UpdateBuffer[i];

                if (!m_SfxReferences.Contains(sfxReference))
                {
                    continue;
                }

                sfxReference.CheckForEventToRaise();
                sfxReference.CheckForLoop();
                sfxReference.UpdateLevels(deltaTime, m_AllVolume.Current, allChanged);
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }

        private async Task PreloadAsync(ulong nameHash)
        {
            if (!m_Preloads.TryBegin(nameHash, out int request))
            {
                return;
            }

            SfxPlayable playable = await m_SfxAssetProvider.AcquireAsync(nameHash);

            // Unloaded while it loaded, and perhaps preloaded again since, by a request that holds it instead.
            if (!m_Preloads.TryHold(nameHash, request, playable))
            {
                m_SfxAssetProvider.Release(nameHash);

                return;
            }

            for (int i = 0; i < playable.Recordings.Count; i++)
            {
                await StemClips.LoadAsync(playable.Recordings[i], () => !m_Preloads.Holds(nameHash));
            }
        }

        private ISfxAsset PickRecording(ulong nameHash, SfxPlayable playable, int position)
        {
            IReadOnlyList<ISfxAsset> recordings = playable.Recordings;

            if (recordings.Count == 1)
            {
                return recordings[0];
            }

            int last = m_LastPicked.TryGetValue(nameHash, out int picked) ? picked : SfxVariation.NONE;
            int index = playable.PlaysInOrder
                            ? SfxVariation.InOrderIndex(recordings.Count, last, position)
                            : SfxVariation.PickIndex(recordings.Count, last, UnityEngine.Random.value);

            m_LastPicked[nameHash] = index;

            return recordings[index];
        }

        private static ISfxReference StoppedReference(ulong nameHash, ISfxAsset sfxAsset)
        {
            ISfxReference stopped = new SfxReference(nameHash, Array.Empty<IStemPlayback>(), sfxAsset, 0f, 1f, 0f, _ => { });

            stopped.Stop();

            return stopped;
        }

        private ISfxReference ScheduleSfx(ulong nameHash, ISfxAsset sfxAsset, float startTime, float volume, float pan, float pitch)
        {
            int stemCount = sfxAsset.Tracks.Count;

            if (stemCount > m_Playbacks.Length)
            {
                stemCount = m_Playbacks.Length;
            }

            while (CountFreeVoices() < stemCount)
            {
                EvictOldestSfx();
            }

            // Every stem is played in this frame, so they start together.
            double          startDspTime = AudioSettings.dspTime + Time.deltaTime;
            double          rate         = sfxAsset.SampleRate;
            IStemPlayback[] stems        = new IStemPlayback[stemCount];

            int voiceIndex = 0;

            for (int i = 0; i < stemCount; i++)
            {
                while (m_VoiceOwners[voiceIndex] != null)
                {
                    voiceIndex++;
                }

                int           voice    = voiceIndex;
                IStemPlayback playback = m_Playbacks[voice];
                AudioSource   source   = playback.Source;

                stems[i] = playback;
                voiceIndex++;

                source.volume    = volume * m_AllVolume.Current;
                source.panStereo = pan;
                source.pitch     = pitch;

                m_Channels.SetReverbSend(voice, sfxAsset.Tracks[i].ReverbSendLevel);

                playback.Play(sfxAsset.Tracks[i].Clip, startTime, sfxAsset.Loop, sfxAsset.LoopStart / rate, sfxAsset.LoopEnd / rate, startDspTime);
            }

            SfxReference sfxRef = new SfxReference(nameHash, stems, sfxAsset, startTime, volume, pan, RemoveSfxReference);

            TakeOwnership(stems, sfxRef);

            m_SfxReferences.Add(sfxRef);

            return sfxRef;
        }

        private int CountFreeVoices()
        {
            int free = 0;

            for (int i = 0; i < m_VoiceOwners.Length; i++)
            {
                if (m_VoiceOwners[i] == null)
                {
                    free++;
                }
            }

            return free;
        }

        private void EvictOldestSfx()
        {
            ISfxReference oldest = m_SfxReferences[0];

            oldest.Stop();
            RemoveSfxReference(oldest);
        }

        private void TakeOwnership(IStemPlayback[] stems, ISfxReference owner)
        {
            for (int i = 0; i < m_Playbacks.Length; i++)
            {
                for (int j = 0; j < stems.Length; j++)
                {
                    if (!ReferenceEquals(m_Playbacks[i], stems[j]))
                    {
                        continue;
                    }

                    m_VoiceOwners[i] = owner;

                    break;
                }
            }
        }

        private void ReleaseVoices(ISfxReference owner)
        {
            for (int i = 0; i < m_VoiceOwners.Length; i++)
            {
                if (!ReferenceEquals(m_VoiceOwners[i], owner))
                {
                    continue;
                }

                m_Playbacks[i].Stop();
                m_VoiceOwners[i] = null;
            }
        }

        private void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;

            m_This.StopAll();
            m_This.Unload(m_Preloads.GetNames());

            DestroyChannels();
        }

        private void DestroyChannels()
        {
            m_Channels?.Destroy();
            m_Channels = null;
        }

        private void RemoveSfxReference(ISfxReference sfxReference)
        {
            if (!m_SfxReferences.Remove(sfxReference))
            {
                return;
            }

            ReleaseReference(sfxReference);
        }

        private void ReleaseReference(ISfxReference sfxReference)
        {
            ReleaseVoices(sfxReference);
            StemClips.UnloadUnused(sfxReference.Asset, IsClipInUse);
            m_SfxAssetProvider.Release(sfxReference.NameHash);
        }

        private bool IsClipInUse(AudioClip clip)
        {
            for (int i = 0; i < m_SfxReferences.Count; i++)
            {
                if (AudioUtils.UsesClip(m_SfxReferences[i].Asset, clip))
                {
                    return true;
                }
            }

            for (int i = 0; i < m_Loading.Count; i++)
            {
                if (AudioUtils.UsesClip(m_Loading[i], clip))
                {
                    return true;
                }
            }

            foreach (SfxPlayable preloaded in m_Preloads.Held)
            {
                for (int i = 0; i < preloaded.Recordings.Count; i++)
                {
                    if (AudioUtils.UsesClip(preloaded.Recordings[i], clip))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
