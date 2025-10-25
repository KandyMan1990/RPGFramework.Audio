using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [CreateAssetMenu(fileName = "SFX Asset Provider", menuName = "RPG Framework/SFX Asset Provider")]
    public class SfxAssetProvider : ScriptableObject, ISfxAssetProvider
    {
        [SerializeField]
        private List<SfxAsset> m_SfxAssets = new List<SfxAsset>();

        ISfxAsset ISfxAssetProvider.GetSfxAsset(int id)
        {
            ISfxAsset sfxAsset = m_SfxAssets[id];

            return sfxAsset;
        }
    }
}