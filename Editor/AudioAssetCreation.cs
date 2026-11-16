using System.Collections.Generic;
using RPGFramework.Audio.Editor.Loops;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Editor
{
    /// <summary>
    /// A song or a sound made from the clips selected, through Assets › Create › RPG Framework › Audio or the project
    /// window's own Create menu on them: its stems the clips in name order, each sending to the reverb, its loop and sample
    /// rate read from their source files. And clips dropped on a song or a sound in the project window added as its stems,
    /// their loop read as the inspector reads it for a stem added.
    /// </summary>
    internal static class AudioAssetCreation
    {
        private const string MENU = "Assets/Create/RPG Framework/Audio/";

        // The Audio menu in name order, other packages' entries in it numbered alike: Battle Audio Provider 0, Music Asset 1,
        // Music Asset Provider 2, SFX Asset 3, SFX Asset Provider 4, SFX Collection 5, System Sound Config 6.
        private const int MUSIC_ASSET_ORDER = 1;
        private const int SFX_ASSET_ORDER   = 3;

        [MenuItem(MENU + "Music Asset", false, MUSIC_ASSET_ORDER)]
        private static void CreateMusicAsset()
        {
            Create<MusicAsset>("Music Asset", true);
        }

        [MenuItem(MENU + "SFX Asset", false, SFX_ASSET_ORDER)]
        private static void CreateSfxAsset()
        {
            Create<SfxAsset>("SFX Asset", false);
        }

        [InitializeOnLoadMethod]
        private static void AcceptProjectDrops()
        {
            DragAndDrop.AddDropHandlerV2(OnProjectDrop);
        }

        private static void Create<T>(string defaultName, bool isSong) where T : ScriptableObject
        {
            AudioClip[] clips = StemTracks.ClipsAmong(Selection.objects);
            T           asset = ScriptableObject.CreateInstance<T>();

            if (clips.Length > 0)
            {
                AddStems(new SerializedObject(asset), clips, isSong);
            }

            ProjectWindowUtil.CreateAsset(asset, $"{StemTracks.NameFor(clips, defaultName)}.asset");
        }

        private static DragAndDropVisualMode OnProjectDrop(EntityId dragEntityId, string dropUponPath, bool perform)
        {
            ScriptableObject asset  = AssetDatabase.LoadAssetAtPath<ScriptableObject>(dropUponPath);
            bool             isSong = asset is MusicAsset;

            if (!isSong && asset is not SfxAsset)
            {
                return DragAndDropVisualMode.None;
            }

            AudioClip[] clips = StemTracks.ClipsAmong(DragAndDrop.objectReferences);

            if (clips.Length == 0)
            {
                return DragAndDropVisualMode.None;
            }

            if (perform)
            {
                DragAndDrop.AcceptDrag();
                AddStems(new SerializedObject(asset), clips, isSong);
            }

            return DragAndDropVisualMode.Copy;
        }

        // A sound takes the loop read, as a song looping by samples or not yet looping does; one looping by bars keeps them.
        private static void AddStems(SerializedObject asset, AudioClip[] clips, bool isSong)
        {
            StemTracks.AddClips(asset, clips);

            IReadOnlyList<IStem> tracks  = ((IAudioAsset)asset.targetObject).Tracks;
            StemLoopReading      reading = StemLoopFields.ReadStems(tracks);

            if (reading.Found && !isSong)
            {
                StemLoopFields.ApplyToSound(asset, reading);
            }
            else if (reading.Found && StemLoopFields.SongTakesLoopAsAdded(asset))
            {
                StemLoopFields.ApplyToSong(asset, reading);
            }

            asset.Update();

            bool counts = !isSong || asset.FindProperty("m_LoopUnit").intValue == (int)LoopUnit.Samples;

            StemLoopFields.CheckRate(asset.FindProperty(isSong ? "m_LoopSampleRate" : "m_SampleRate"), tracks, counts);
        }
    }
}
