using System.Collections.Generic;
using RPGFramework.Audio.Music;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace RPGFramework.Audio.Music_Sample
{
    public class MusicSample : MonoBehaviour
    {
        [SerializeField]
        private MusicAssetProvider m_MusicAssetProvider;
        [SerializeField]
        private AudioMixerGroup[] m_MusicMixerGroups;
        
        private IMusicPlayer m_MusicPlayer;

        private void Start()
        {
            m_MusicPlayer = new UnityMusicPlayer();
            m_MusicPlayer.SetMusicAssetProvider(m_MusicAssetProvider);
            m_MusicPlayer.SetStemMixerGroups(m_MusicMixerGroups);
        }

        private void Update()
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                m_MusicPlayer.Play(0);
            }
            
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                m_MusicPlayer.Play(0);
                m_MusicPlayer.SetActiveStemsImmediate(new Dictionary<int, bool>
                                                      {
                                                              { 0, false },
                                                              { 1, true },
                                                              { 2, true },
                                                              { 3, true }
                                                      });
            }
            
            if (Keyboard.current.eKey.wasPressedThisFrame)
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
            
            if (Keyboard.current.rKey.wasPressedThisFrame)
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
            
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                m_MusicPlayer.Pause();
            }
            
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                m_MusicPlayer.Stop();
            }
            
            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                m_MusicPlayer.Stop(2f);
            }
        }
    }
}