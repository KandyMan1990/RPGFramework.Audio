using System;
using System.Collections.Generic;
using RPGFramework.Audio.Editor.Loops;
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

        private const string LOOP_UNIT         = "m_LoopUnit";
        private const string LOOP_START_SAMPLE = "m_LoopStartSample";
        private const string LOOP_END_SAMPLE   = "m_LoopEndSample";
        private const string LOOP_SAMPLE_RATE  = "m_LoopSampleRate";
        private const string LOOP_START_BAR    = "m_LoopStartBar";
        private const string LOOP_END_BAR      = "m_LoopEndBar";

        private readonly AudioStemPreview m_Preview = new AudioStemPreview();

        private SamplePointRow  m_LoopStart;
        private SamplePointRow  m_LoopEnd;
        private VisualElement[] m_SampleFields;
        private VisualElement[] m_BarFields;
        private HelpBox         m_RateHelp;
        private HelpBox         m_ReadHelp;
        private Button          m_UseStemsLoop;
        private AudioClip[]     m_StemClips;
        private StemLoopReading m_Offered;

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
            BuildLoopFields(root);

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

            m_Play = new Button(() => Play(0.0))
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

            m_StemClips = StemLoopFields.CurrentClips(Music.Tracks);
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

        /// <summary>
        /// The loop's sample fields as rows with the same point in seconds and the bar it falls in, the rate's help box
        /// under the rate, and the unit's fields shown while the other unit's are hidden.
        /// </summary>
        private void BuildLoopFields(VisualElement root)
        {
            m_LoopStart = new SamplePointRow(serializedObject.FindProperty(LOOP_START_SAMPLE), "Loop Start Sample", LOOP_SAMPLE_RATE, true);
            m_LoopEnd   = new SamplePointRow(serializedObject.FindProperty(LOOP_END_SAMPLE), "Loop End Sample", LOOP_SAMPLE_RATE, true);
            m_RateHelp  = new HelpBox(string.Empty, HelpBoxMessageType.Warning);

            StemLoopFields.Replace(root, LOOP_START_SAMPLE, m_LoopStart.Root);
            StemLoopFields.Replace(root, LOOP_END_SAMPLE, m_LoopEnd.Root);

            PropertyField rate = StemLoopFields.FindField(root, LOOP_SAMPLE_RATE);
            rate.parent.Insert(rate.parent.IndexOf(rate) + 1, m_RateHelp);

            m_SampleFields = new[] { m_LoopStart.Root, m_LoopEnd.Root, rate, m_RateHelp };
            m_BarFields    = new VisualElement[] { StemLoopFields.FindField(root, LOOP_START_BAR), StemLoopFields.FindField(root, LOOP_END_BAR) };

            Button read = new Button(() => ReadStems(false))
                          {
                              text    = "Read loop from stems",
                              tooltip = "Read the loop from the stems' source files — two markers named LOOPSTART and LOOPEND, or a sampler loop, in a WAV; LOOPSTART with LOOPLENGTH or LOOPEND, or alone, in an OGG — as it is read when a stem is added. For a stem exported again"
                          };

            m_ReadHelp     = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            m_UseStemsLoop = new Button(() => Apply(m_Offered))
                             {
                                 text    = "Use the stems' loop",
                                 tooltip = "Loop by the samples the stems' files mark, in place of the bars"
                             };

            PropertyField endBar = StemLoopFields.FindField(root, LOOP_END_BAR);
            int           at     = endBar.parent.IndexOf(endBar) + 1;

            endBar.parent.Insert(at, read);
            endBar.parent.Insert(at + 1, m_ReadHelp);
            endBar.parent.Insert(at + 2, m_UseStemsLoop);
            m_ReadHelp.style.display     = DisplayStyle.None;
            m_UseStemsLoop.style.display = DisplayStyle.None;
        }

        /// <summary>The stems' loop read when a stem's clip has been set since the asset was last seen.</summary>
        private void ReadIfAStemWasSet()
        {
            AudioClip[] clips = StemLoopFields.CurrentClips(Music.Tracks);
            bool        set   = StemLoopFields.AStemWasSet(m_StemClips, clips);

            m_StemClips = clips;

            if (set)
            {
                ReadStems(true);
            }
        }

        /// <summary>
        /// Reads every stem's source file for the loop. Asked for, or as a stem is added to a song with no loop yet or one
        /// looping by samples, it is written; added to a song looping by bars, it is offered.
        /// </summary>
        private void ReadStems(bool asAdded)
        {
            StemLoopReading reading = StemLoopFields.ReadStems(Music.Tracks);

            m_Offered = null;

            if (reading.Found)
            {
                serializedObject.Update();

                bool     loops = serializedObject.FindProperty("m_Loop").boolValue;
                LoopUnit unit  = (LoopUnit)serializedObject.FindProperty(LOOP_UNIT).intValue;

                if (!asAdded || StemLoopReading.AppliesWhenAdded(loops, unit))
                {
                    Apply(reading);
                }
                else
                {
                    m_Offered = reading;
                }
            }

            m_ReadHelp.text              = m_Offered != null ? $"{reading.Report} The song loops by bars, so its loop is offered rather than changed." : reading.Report;
            m_ReadHelp.messageType       = reading.Refused ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
            m_ReadHelp.style.display     = DisplayStyle.Flex;
            m_UseStemsLoop.style.display = m_Offered != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Apply(StemLoopReading reading)
        {
            if (reading == null || !reading.Found)
            {
                return;
            }

            serializedObject.Update();
            serializedObject.FindProperty("m_Loop").boolValue         = true;
            serializedObject.FindProperty(LOOP_UNIT).intValue         = (int)LoopUnit.Samples;
            serializedObject.FindProperty(LOOP_START_SAMPLE).intValue = reading.LoopStart;
            serializedObject.FindProperty(LOOP_END_SAMPLE).intValue   = reading.LoopEnd;
            serializedObject.FindProperty(LOOP_SAMPLE_RATE).intValue  = reading.SampleRate;
            serializedObject.ApplyModifiedProperties();

            m_Offered                    = null;
            m_UseStemsLoop.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// The unit's fields, the seconds and bars beside the samples, and the rate: filled from the first stem's source
        /// file while it is unset, and a help box when it differs from that file's, as the numbers are never changed
        /// under an author.
        /// </summary>
        private void RefreshLoopFields()
        {
            serializedObject.Update();

            SerializedProperty rate    = serializedObject.FindProperty(LOOP_SAMPLE_RATE);
            bool               samples = serializedObject.FindProperty(LOOP_UNIT).intValue == (int)LoopUnit.Samples;
            string             help    = StemLoopFields.CheckRate(rate, Music.Tracks, samples);

            for (int i = 0; i < m_SampleFields.Length; i++)
            {
                m_SampleFields[i].style.display = samples ? DisplayStyle.Flex : DisplayStyle.None;
            }

            for (int i = 0; i < m_BarFields.Length; i++)
            {
                m_BarFields[i].style.display = samples ? DisplayStyle.None : DisplayStyle.Flex;
            }

            m_RateHelp.text          = help ?? string.Empty;
            m_RateHelp.style.display = samples && help != null ? DisplayStyle.Flex : DisplayStyle.None;

            m_LoopStart.SetBar(BarOf(m_LoopStart.Refresh(rate.intValue), rate.intValue));
            m_LoopEnd.SetBar(BarOf(m_LoopEnd.Refresh(rate.intValue), rate.intValue));
        }

        private void Rebuild()
        {
            ReadIfAStemWasSet();
            RefreshLoopFields();

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
            Play(Math.Max(0.0, Music.LoopEndTime - LEAD_IN_SECONDS));
        }

        private void Play(double startSeconds)
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

            m_Preview.Play(tracks, m_Active, startSeconds, Music.Loop ? Music.LoopStartTime : 0.0, Music.Loop ? Music.LoopEndTime : 0.0);
        }

        private void RefreshPosition()
        {
            IReadOnlyList<IStem> tracks = Music.Tracks;
            string               loop   = DescribeLoop();

            if (!m_Preview.IsPlaying || tracks == null || tracks.Count == 0 || tracks[0].Clip == null)
            {
                m_Position.text = $"Stopped, {loop}";

                return;
            }

            AudioClip clip    = tracks[0].Clip;
            float     seconds = (float)m_Preview.Position;

            m_Position.text = $"{FormatTime(seconds)} of {FormatTime(clip.length)}{FormatBar(seconds)}, {loop}";
        }

        private string DescribeLoop()
        {
            if (!Music.Loop)
            {
                return "plays through";
            }

            if (serializedObject.FindProperty(LOOP_UNIT).intValue == (int)LoopUnit.Bars)
            {
                return $"loops bars {serializedObject.FindProperty(LOOP_START_BAR).intValue} to {serializedObject.FindProperty(LOOP_END_BAR).intValue}";
            }

            return $"loops {FormatTime((float)Music.LoopStartTime)} to {FormatTime((float)Music.LoopEndTime)}";
        }

        /// <returns>The bar the playhead is in, counted from 1 as the loop's bars are, or nothing without a tempo.</returns>
        private string FormatBar(float seconds)
        {
            string bar = ((MusicAsset)target).TryGetBar(seconds, out int number) ? $", bar {number}" : string.Empty;

            return bar;
        }

        private string BarOf(double seconds, int rate)
        {
            return rate > 0 && ((MusicAsset)target).TryGetBar(seconds, out int bar) ? $"bar {bar}" : string.Empty;
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
