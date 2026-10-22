using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    [CreateAssetMenu(fileName = "Music Asset Provider", menuName = "RPG Framework/Audio/Music Asset Provider")]
    public sealed class MusicAssetProvider : ScriptableObject, IMusicAssetProvider
#if ENABLE_RPG_FRAMEWORK_DEBUG
        , IDebugMusicAssetProvider
#endif
    {
        [SerializeField]
        private MusicAsset[] m_MusicAssets = Array.Empty<MusicAsset>();

        private Dictionary<ulong, MusicAsset> m_ByNameHash;

#if UNITY_EDITOR
        public IEnumerable<string> AssetNames
        {
            get
            {
                for (int i = 0; i < m_MusicAssets.Length; i++)
                {
                    MusicAsset asset = m_MusicAssets[i];

                    if (asset == null)
                    {
                        continue;
                    }

                    yield return asset.name;
                }
            }
        }

        public IEnumerable<string> StemStateNamesOf(string assetName)
        {
            for (int i = 0; i < m_MusicAssets.Length; i++)
            {
                MusicAsset asset = m_MusicAssets[i];

                if (asset == null || asset.name != assetName)
                {
                    continue;
                }

                return asset.StemStateNames;
            }

            return System.Array.Empty<string>();
        }

        public IEnumerable<string> StemStateNames
        {
            get
            {
                for (int i = 0; i < m_MusicAssets.Length; i++)
                {
                    MusicAsset asset = m_MusicAssets[i];

                    if (asset == null)
                    {
                        continue;
                    }

                    foreach (string stateName in asset.StemStateNames)
                    {
                        yield return stateName;
                    }
                }
            }
        }
#endif

        // Every song is in the build already, so there is nothing to load or unload.
        Task<MusicAsset> IMusicAssetProvider.AcquireAsync(ulong nameHash)
        {
            if (!m_ByNameHash.TryGetValue(nameHash, out MusicAsset musicAsset))
            {
                throw new KeyNotFoundException($"{nameof(MusicAssetProvider)}::{nameof(IMusicAssetProvider.AcquireAsync)} [{name}] has no song whose name hashes to [{nameHash}]");
            }

            Task<MusicAsset> acquired = Task.FromResult(musicAsset);

            return acquired;
        }

        void IMusicAssetProvider.Release(ulong nameHash)
        {
        }

#if ENABLE_RPG_FRAMEWORK_DEBUG
        Task<IReadOnlyList<string>> IDebugMusicAssetProvider.LoadNamesAsync()
        {
            List<string> names = new List<string>(m_MusicAssets.Length);

            for (int i = 0; i < m_MusicAssets.Length; i++)
            {
                if (m_MusicAssets[i] != null)
                {
                    names.Add(m_MusicAssets[i].name);
                }
            }

            return Task.FromResult<IReadOnlyList<string>>(names);
        }

        Task<IReadOnlyList<string>> IDebugMusicAssetProvider.LoadStemStateNamesAsync(ulong nameHash)
        {
            IReadOnlyList<string> names = new List<string>(m_ByNameHash[nameHash].StemStateNames);

            return Task.FromResult(names);
        }
#endif

        private void OnEnable()
        {
            BuildLookup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            BuildLookup();
        }
#endif

        private void BuildLookup()
        {
            m_ByNameHash = new Dictionary<ulong, MusicAsset>(m_MusicAssets.Length);

            for (int i = 0; i < m_MusicAssets.Length; i++)
            {
                MusicAsset musicAsset = m_MusicAssets[i];

                if (musicAsset == null)
                {
                    continue;
                }

                m_ByNameHash[Fnv1a64.Hash(musicAsset.name)] = musicAsset;
            }
        }
    }
}
