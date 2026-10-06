using System.Collections.Generic;
using RPGFramework.Audio.Editor.Preview;
using RPGFramework.Audio.Music;
using RPGFramework.Hashing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    [CustomEditor(typeof(MusicAsset))]
    internal sealed class MusicAssetEditor : UnityEditor.Editor
    {
        // How much of the song plays before its loop's end, to hear the jump back to the start.
        private const float LEAD_IN_SECONDS = 4f;

        private readonly AudioStemPreview m_Preview = new AudioStemPreview();

        private Button        m_Play;
        private Button        m_PlayIntoLoop;
        private Label         m_Position;
        private DropdownField m_State;
        private VisualElement m_Stems;
        private Toggle[]      m_StemToggles;
        private bool[]        m_Active;

        private IMusicAsset Music => (MusicAsset)target;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            root.Add(new Label("Preview")
                     {
                         style =
                         {
                             unityFontStyleAndWeight = FontStyle.Bold,
                             marginTop               = 8
                         }
                     });

            VisualElement buttons = new VisualElement
                                    {
                                        style =
                                        {
                                            flexDirection = FlexDirection.Row
                                        }
                                    };

            m_Play = new Button(() => Play(0f))
                     {
                         text = "Play"
                     };
            m_PlayIntoLoop = new Button(PlayIntoLoop)
                             {
                                 text    = "Play into the loop",
                                 tooltip = $"Play from {LEAD_IN_SECONDS} seconds before the loop's end, to hear the jump back to its start"
                             };

            buttons.Add(Grow(m_Play));
            buttons.Add(Grow(m_PlayIntoLoop));
            buttons.Add(Grow(new Button(m_Preview.Stop)
                             {
                                 text = "Stop"
                             }));
            root.Add(buttons);

            m_Position = new Label();
            root.Add(m_Position);

            m_State = new DropdownField("Stem state");
            m_State.RegisterValueChangedCallback(evt => ApplyState(evt.newValue));
            root.Add(m_State);

            m_Stems = new VisualElement();
            root.Add(m_Stems);

            Rebuild();

            // A change to the asset can change its stems, states or loop, which a preview under way would not follow.
            root.TrackSerializedObjectValue(serializedObject, _ =>
                                                              {
                                                                  m_Preview.Stop();
                                                                  Rebuild();
                                                              });
            root.schedule.Execute(RefreshPosition).Every(50);

            return root;
        }

        private void OnDisable()
        {
            m_Preview.Stop();
        }

        private void Rebuild()
        {
            IReadOnlyList<IStem> tracks = Music.Tracks;
            int                  count  = tracks?.Count ?? 0;
            List<string>         states = new List<string>(((MusicAsset)target).StemStateNames);

            m_State.choices = states;
            m_State.SetValueWithoutNotify(states.Count > 0 ? states[0] : string.Empty);

            m_Active = new bool[count];

            for (int i = 0; i < count; i++)
            {
                m_Active[i] = true;
            }

            m_Stems.Clear();
            m_StemToggles = new Toggle[count];

            for (int i = 0; i < count; i++)
            {
                int    stem   = i;
                Toggle toggle = new Toggle(tracks[i].Clip != null ? tracks[i].Clip.name : $"Stem {i + 1}, no clip");

                toggle.RegisterValueChangedCallback(evt =>
                                                    {
                                                        m_Active[stem] = evt.newValue;
                                                        m_Preview.SetStemActive(stem, evt.newValue);
                                                    });

                m_StemToggles[i] = toggle;
                m_Stems.Add(toggle);
            }

            if (states.Count > 0)
            {
                ApplyState(states[0]);
            }

            m_Play.SetEnabled(count > 0);
            m_PlayIntoLoop.SetEnabled(count > 0 && Music.Loop);
        }

        private void ApplyState(string stateName)
        {
            bool[] stems = Music.GetStemsForState(Fnv1a64.Hash(stateName));

            for (int i = 0; i < m_Active.Length && i < stems.Length; i++)
            {
                m_Active[i] = stems[i];
                m_StemToggles[i].SetValueWithoutNotify(stems[i]);
                m_Preview.SetStemActive(i, stems[i]);
            }
        }

        private void PlayIntoLoop()
        {
            Play(Mathf.Max(0f, (float)Music.LoopEndTime - LEAD_IN_SECONDS));
        }

        private void Play(float startSeconds)
        {
            IReadOnlyList<IStem> tracks = Music.Tracks;

            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].Clip == null)
                {
                    Debug.LogWarning($"{nameof(MusicAssetEditor)}::{nameof(Play)} Stem [{i + 1}] of [{target.name}] has no clip, so it cannot be previewed");

                    return;
                }
            }

            int frequency = tracks[0].Clip.frequency;
            int loopStart = Music.Loop ? (int)(Music.LoopStartTime * frequency) : 0;
            int loopEnd   = Music.Loop ? (int)(Music.LoopEndTime   * frequency) : 0;

            m_Preview.Play(tracks, m_Active, (int)(startSeconds * frequency), loopStart, loopEnd);
        }

        private void RefreshPosition()
        {
            IReadOnlyList<IStem> tracks = Music.Tracks;
            string               loop   = Music.Loop ? $"loops bars {serializedObject.FindProperty("m_LoopStartBar").intValue} to {serializedObject.FindProperty("m_LoopEndBar").intValue}" : "plays through";

            if (!m_Preview.IsPlaying || tracks == null || tracks.Count == 0 || tracks[0].Clip == null)
            {
                m_Position.text = $"Stopped, {loop}";

                return;
            }

            AudioClip clip    = tracks[0].Clip;
            float     seconds = (float)m_Preview.Position / clip.frequency;

            m_Position.text = $"{FormatTime(seconds)} of {FormatTime(clip.length)}{FormatBar(seconds)}, {loop}";
        }

        /// <returns>The bar the playhead is in, counted from 1 as the loop's bars are, or nothing without a tempo.</returns>
        private string FormatBar(float seconds)
        {
            float bpm         = serializedObject.FindProperty("m_BPM").floatValue;
            int   beatsPerBar = serializedObject.FindProperty("m_BeatsPerBar").intValue;
            int   beatUnit    = serializedObject.FindProperty("m_BeatUnit").intValue;

            if (bpm <= 0f || beatsPerBar <= 0 || beatUnit <= 0)
            {
                return string.Empty;
            }

            double secondsPerBar = beatsPerBar * (4.0 / beatUnit) * (60.0 / bpm);
            string bar           = $", bar {(int)(seconds / secondsPerBar) + 1}";

            return bar;
        }

        private static string FormatTime(float seconds)
        {
            string time = $"{(int)(seconds / 60f)}:{seconds % 60f:00.0}";

            return time;
        }

        private static Button Grow(Button button)
        {
            button.style.flexGrow = 1;

            return button;
        }
    }
}
