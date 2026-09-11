using System.Collections.Generic;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [CreateAssetMenu(fileName = "SFX Asset Provider", menuName = "RPG Framework/Audio/SFX Asset Provider")]
    public class SfxAssetProvider : ScriptableObject, ISfxAssetProvider
    {
        [SerializeField]
        private List<SfxAsset> m_SfxAssets = new List<SfxAsset>();

        private Dictionary<ulong, SfxAsset> m_ByNameHash;

#if UNITY_EDITOR
        public IEnumerable<string> AssetNames
        {
            get
            {
                foreach (SfxAsset asset in m_SfxAssets)
                {
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
            m_ByNameHash = new Dictionary<ulong, SfxAsset>(m_SfxAssets.Count);

            foreach (SfxAsset sfxAsset in m_SfxAssets)
            {
                if (sfxAsset == null)
                {
                    continue;
                }

                m_ByNameHash[Fnv1a64.Hash(sfxAsset.name)] = sfxAsset;
            }
        }
    }
}
