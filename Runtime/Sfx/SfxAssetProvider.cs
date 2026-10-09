using System;
using System.Collections.Generic;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [CreateAssetMenu(fileName = "SFX Asset Provider", menuName = "RPG Framework/Audio/SFX Asset Provider")]
    public class SfxAssetProvider : ScriptableObject, ISfxAssetProvider
    {
        [SerializeField]
        private SfxAsset[] m_SfxAssets = Array.Empty<SfxAsset>();

        private Dictionary<ulong, SfxAsset> m_ByNameHash;

#if UNITY_EDITOR
        public IEnumerable<string> AssetNames
        {
            get
            {
                for (int i = 0; i < m_SfxAssets.Length; i++)
                {
                    SfxAsset asset = m_SfxAssets[i];

                    if (asset == null)
                    {
                        continue;
                    }

                    yield return asset.name;
                }
            }
        }
#endif

        ISfxAsset ISfxAssetProvider.GetSfxAsset(ulong nameHash)
        {
            SfxAsset sfxAsset = m_ByNameHash[nameHash];

            return sfxAsset;
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
            m_ByNameHash = new Dictionary<ulong, SfxAsset>(m_SfxAssets.Length);

            for (int i = 0; i < m_SfxAssets.Length; i++)
            {
                SfxAsset sfxAsset = m_SfxAssets[i];

                if (sfxAsset == null)
                {
                    continue;
                }

                m_ByNameHash[Fnv1a64.Hash(sfxAsset.name)] = sfxAsset;
            }
        }
    }
}
