using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    [CreateAssetMenu(fileName = "Music Asset Provider", menuName = "RPG Framework/Music Asset Provider")]
    public class MusicAssetProvider : ScriptableObject, IMusicAssetProvider
    {
        [SerializeField]
        private List<MusicAsset> m_MusicAssets = new List<MusicAsset>();

        IMusicAsset IMusicAssetProvider.GetMusicAsset(int id)
        {
            IMusicAsset musicAsset = m_MusicAssets[id];
            musicAsset.CalculateLoopPoints();

            return musicAsset;
        }
    }
}