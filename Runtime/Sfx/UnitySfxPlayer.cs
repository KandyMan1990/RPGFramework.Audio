using System;
using System.Collections.Generic;
using System.Linq;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Sfx
{
    public class UnitySfxPlayer : ISfxPlayer, IUpdatable
    {
        private ISfxAssetProvider m_SfxAssetProvider;
        private AudioSource[]     m_CurrentSources;
        private AudioMixerGroup[] m_StemMixerGroups;
        private AudioMixer        m_AudioMixer;

        private readonly List<ISfxReference> m_SfxReferences;

        private const string SEND_SUFFIX = "_Send";

        public UnitySfxPlayer()
        {
            m_SfxReferences = new List<ISfxReference>();

            UpdateManager.RegisterUpdatable(this);
        }

        ~UnitySfxPlayer()
        {
            UpdateManager.UnregisterUpdatable(this);
        }

        ISfxReference ISfxPlayer.Play(int id)
        {
            return ScheduleSfx(id, 0f);
        }

        void ISfxPlayer.Pause(ISfxReference sfxReference)
        {
            sfxReference.Pause();
        }

        void ISfxPlayer.PauseAll()
        {
            foreach (ISfxReference sfxReference in m_SfxReferences)
            {
                ((ISfxPlayer)this).Pause(sfxReference);
            }
        }

        void ISfxPlayer.Resume(ISfxReference sfxReference)
        {
            sfxReference.Resume();
        }

        void ISfxPlayer.ResumeAll()
        {
            foreach (ISfxReference sfxReference in m_SfxReferences)
            {
                ((ISfxPlayer)this).Resume(sfxReference);
            }
        }

        void ISfxPlayer.Stop(ISfxReference sfxReference)
        {
            if (!m_SfxReferences.Contains(sfxReference))
            {
                return;
            }

            sfxReference.Stop();
            RemoveSfxReference(sfxReference);
        }

        void ISfxPlayer.StopAll()
        {
            List<ISfxReference> sfxReferences = new List<ISfxReference>(m_SfxReferences);

            foreach (ISfxReference sfxReference in sfxReferences)
            {
                ((ISfxPlayer)this).Stop(sfxReference);
            }

            m_SfxReferences.Clear();
        }

        void ISfxPlayer.SetSfxAssetProvider(ISfxAssetProvider provider)
        {
            m_SfxAssetProvider = provider;
        }

        void ISfxPlayer.SetStemMixerGroups(AudioMixerGroup[] groups)
        {
            m_StemMixerGroups = groups;
            m_AudioMixer      = m_StemMixerGroups[0].audioMixer;

            m_CurrentSources = new AudioSource[m_StemMixerGroups.Length];

            GameObject sfxPlayer = new GameObject("SfxPlayer");
            UnityEngine.Object.DontDestroyOnLoad(sfxPlayer);

            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                GameObject go = new GameObject(m_StemMixerGroups[i].name);
                go.transform.parent                       = sfxPlayer.transform;
                m_CurrentSources[i]                       = go.AddComponent<AudioSource>();
                m_CurrentSources[i].outputAudioMixerGroup = m_StemMixerGroups[i];
            }
        }

        void IUpdatable.Update()
        {
            if (m_SfxReferences.Count == 0)
            {
                return;
            }

            List<ISfxReference> sfxReferences = new List<ISfxReference>(m_SfxReferences);

            foreach (ISfxReference sfxReference in sfxReferences)
            {
                sfxReference.CheckForEventToRaise();
                sfxReference.CheckForLoop();
            }
        }

        private void RemoveSfxReference(ISfxReference sfxReference)
        {
            m_SfxReferences.Remove(sfxReference);
        }

        private ISfxReference ScheduleSfx(int id, float startTime)
        {
            ISfxAsset sfxAsset = m_SfxAssetProvider.GetSfxAsset(id);

            double        scheduledStartTime    = AudioSettings.dspTime + Time.deltaTime;
            AudioSource[] audioSourceReferences = new AudioSource[sfxAsset.Tracks.Count];

            for (int i = 0; i < sfxAsset.Tracks.Count; i++)
            {
                AudioSource source          = m_CurrentSources.First(s => !s.isPlaying);
                int         mixerGroupIndex = Array.IndexOf(m_CurrentSources, source);

                audioSourceReferences[i] = source;

                source.clip                  = sfxAsset.Tracks[i].Clip;
                source.playOnAwake           = false;
                source.loop                  = false;
                source.volume                = 1f;
                source.time                  = startTime;
                source.outputAudioMixerGroup = m_StemMixerGroups[mixerGroupIndex];

                float sendLevel = math.lerp(-10f, 0f, sfxAsset.Tracks[i].ReverbSendLevel);
                m_AudioMixer.SetFloat($"{m_StemMixerGroups[mixerGroupIndex].name}{SEND_SUFFIX}", sendLevel);

                source.PlayScheduled(scheduledStartTime);
            }

            SfxReference sfxRef = new SfxReference(audioSourceReferences, sfxAsset, RemoveSfxReference);

            m_SfxReferences.Add(sfxRef);

            return sfxRef;
        }
    }
}