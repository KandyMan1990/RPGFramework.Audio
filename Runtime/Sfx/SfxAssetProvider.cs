using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [CreateAssetMenu(fileName = "SFX Asset Provider", menuName = "RPG Framework/Audio/SFX Asset Provider")]
    public sealed class SfxAssetProvider : ScriptableObject, ISfxAssetProvider
#if ENABLE_RPG_FRAMEWORK_DEBUG
        , IDebugSfxAssetProvider
#endif
    {
        [SerializeField]
        private SfxPlayable[] m_SfxAssets = Array.Empty<SfxPlayable>();

        private Dictionary<ulong, SfxPlayable> m_ByNameHash;

#if UNITY_EDITOR
        public IEnumerable<string> AssetNames
        {
            get
            {
                for (int i = 0; i < m_SfxAssets.Length; i++)
                {
                    SfxPlayable asset = m_SfxAssets[i];

                    if (asset == null)
                    {
                        continue;
                    }

                    yield return asset.name;
                }
            }
        }
#endif

        // Every sound is in the build already, so there is nothing to load or unload.
        Task<SfxPlayable> ISfxAssetProvider.AcquireAsync(ulong nameHash)
        {
            if (!m_ByNameHash.TryGetValue(nameHash, out SfxPlayable sfxAsset))
            {
                throw new KeyNotFoundException($"{nameof(SfxAssetProvider)}::{nameof(ISfxAssetProvider.AcquireAsync)} [{name}] has no sound whose name hashes to [{nameHash}]");
            }

            return Task.FromResult(sfxAsset);
        }

        void ISfxAssetProvider.Release(ulong nameHash)
        {
        }

#if ENABLE_RPG_FRAMEWORK_DEBUG
        Task<IReadOnlyList<string>> IDebugSfxAssetProvider.LoadNamesAsync()
        {
            List<string> names = new List<string>(m_SfxAssets.Length);

            for (int i = 0; i < m_SfxAssets.Length; i++)
            {
                if (m_SfxAssets[i] != null)
                {
                    names.Add(m_SfxAssets[i].name);
                }
            }

            return Task.FromResult<IReadOnlyList<string>>(names);
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
            m_ByNameHash = new Dictionary<ulong, SfxPlayable>(m_SfxAssets.Length);

            for (int i = 0; i < m_SfxAssets.Length; i++)
            {
                SfxPlayable sfxAsset = m_SfxAssets[i];

                if (sfxAsset == null)
                {
                    continue;
                }

                m_ByNameHash[Fnv1a64.Hash(sfxAsset.name)] = sfxAsset;
            }
        }
    }
}