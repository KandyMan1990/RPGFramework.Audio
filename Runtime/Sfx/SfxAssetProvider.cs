using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [CreateAssetMenu(fileName = "SFX Asset Provider", menuName = "RPG Framework/Audio/SFX Asset Provider", order = 4)]
    public sealed class SfxAssetProvider : ScriptableObject, ISfxAssetProvider
#if ENABLE_RPG_FRAMEWORK_DEBUG
        , IDebugSfxAssetProvider
#endif
    {
        [SerializeField]
        private SfxPlayable[] m_SfxAssets = Array.Empty<SfxPlayable>();

        private AudioAssetDatabase<SfxPlayable> m_Database;

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
            return Task.FromResult(m_Database.Get(nameHash));
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
            BuildDatabase();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            BuildDatabase();
        }
#endif

        private void BuildDatabase()
        {
            m_Database = new AudioAssetDatabase<SfxPlayable>(m_SfxAssets, $"{nameof(SfxAssetProvider)} [{name}]", "sound");
        }
    }
}
