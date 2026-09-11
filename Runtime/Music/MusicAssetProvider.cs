using System.Collections.Generic;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    [CreateAssetMenu(fileName = "Music Asset Provider", menuName = "RPG Framework/Audio/Music Asset Provider")]
    public class MusicAssetProvider : ScriptableObject, IMusicAssetProvider
    {
        [SerializeField]
        private List<MusicAsset> m_MusicAssets = new List<MusicAsset>();

        private Dictionary<ulong, MusicAsset> m_ByNameHash;

#if UNITY_EDITOR
        public IEnumerable<string> AssetNames
        {
            get
            {
                foreach (MusicAsset asset in m_MusicAssets)
                {
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
            foreach (MusicAsset asset in m_MusicAssets)
            {
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
                foreach (MusicAsset asset in m_MusicAssets)
                {
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

        IMusicAsset IMusicAssetProvider.GetMusicAsset(ulong nameHash)
        {
            IMusicAsset musicAsset = m_ByNameHash[nameHash];

            return musicAsset;
        }

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
            m_ByNameHash = new Dictionary<ulong, MusicAsset>(m_MusicAssets.Count);

            foreach (MusicAsset musicAsset in m_MusicAssets)
            {
                if (musicAsset == null)
                {
                    continue;
                }

                m_ByNameHash[Fnv1a64.Hash(musicAsset.name)] = musicAsset;
            }
        }
    }
}
