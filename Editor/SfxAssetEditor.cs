using System.Collections.Generic;
using RPGFramework.Audio.Editor.Loops;
using RPGFramework.Audio.Editor.Preview;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    [CustomEditor(typeof(SfxAsset))]
    internal sealed class SfxAssetEditor : UnityEditor.Editor
    {
        private const string LOOP_START  = "m_LoopStart";
        private const string LOOP_END    = "m_LoopEnd";
        private const string SAMPLE_RATE = "m_SampleRate";

        private readonly AudioStemPreview m_Preview = new AudioStemPreview();

        private SamplePointRow m_LoopStart;
        private SamplePointRow m_LoopEnd;
        private HelpBox        m_RateHelp;
        private HelpBox        m_ReadHelp;
        private AudioClip[]    m_StemClips;

        private Button        m_Play;
        private Label         m_Position;
        private VisualElement m_Events;

        // The asset's events, and the completion the player raises after a sound that does not loop, as it publishes them.
        private Label[]  m_EventLabels;
        private string[] m_EventNames;
        private double[] m_EventSeconds;
        private bool[]   m_FiresOnce;
        private bool[]   m_Fired;

        private ISfxAsset Sfx => (SfxAsset)target;

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

            m_Play = new Button(Play)
                     {
                         text = "Play",
                         style =
                         {
                             flexGrow = 1
                         }
                     };

            buttons.Add(m_Play);
            buttons.Add(new Button(m_Preview.Stop)
                        {
                            text = "Stop",
                            style =
                            {
                                flexGrow = 1
                            }
                        });
            root.Add(buttons);

            m_Position = new Label();
            root.Add(m_Position);

            m_Events = new VisualElement();
            root.Add(m_Events);

            m_StemClips = StemLoopFields.CurrentClips(Sfx.Tracks);
            Rebuild();

            StemTracks.AcceptDrops(root, serializedObject);

            // A change to the asset can change its stems, events or loop, which a preview under way would not follow.
            root.TrackSerializedObjectValue(serializedObject, _ =>
                                                              {
                                                                  m_Preview.Stop();
                                                                  Rebuild();
                                                              });
            root.schedule.Execute(Refresh).Every(50);

            return root;
        }

        private void OnEnable()
        {
            m_Preview.Advanced += FireEventsReached;
            m_Preview.Looped   += RearmEvents;
            m_Preview.Ended    += FireRemainingEvents;
        }

        private void OnDisable()
        {
            m_Preview.Stop();

            m_Preview.Advanced -= FireEventsReached;
            m_Preview.Looped   -= RearmEvents;
            m_Preview.Ended    -= FireRemainingEvents;
        }

        /// <summary>The loop's fields as rows with the same point in seconds, the rate's help box, and the loop's reading.</summary>
        private void BuildLoopFields(VisualElement root)
        {
            m_LoopStart = new SamplePointRow(serializedObject.FindProperty(LOOP_START), "Loop Start", SAMPLE_RATE, false);
            m_LoopEnd   = new SamplePointRow(serializedObject.FindProperty(LOOP_END), "Loop End", SAMPLE_RATE, false);
            m_RateHelp  = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            m_ReadHelp  = new HelpBox(string.Empty, HelpBoxMessageType.Info);

            StemLoopFields.Replace(root, LOOP_START, m_LoopStart.Root);
            StemLoopFields.Replace(root, LOOP_END, m_LoopEnd.Root);

            Button read = new Button(ReadStems)
                          {
                              text    = "Read loop from stems",
                              tooltip = "Read the loop from the stems' source files — two markers named LOOPSTART and LOOPEND, or a sampler loop, in a WAV; LOOPSTART with LOOPLENGTH or LOOPEND, or alone, in an OGG — as it is read when a stem is added. For a stem exported again"
                          };

            PropertyField rate = StemLoopFields.FindField(root, SAMPLE_RATE);
            int           at   = rate.parent.IndexOf(rate) + 1;

            rate.parent.Insert(at, m_RateHelp);
            rate.parent.Insert(at + 1, read);
            rate.parent.Insert(at + 2, m_ReadHelp);
            m_ReadHelp.style.display = DisplayStyle.None;
        }

        /// <summary>The stems' loop read when a stem's clip has been set since the asset was last seen.</summary>
        private void ReadIfAStemWasSet()
        {
            StemTracks.DefaultNewStems(serializedObject, m_StemClips.Length);

            AudioClip[] clips = StemLoopFields.CurrentClips(Sfx.Tracks);
            bool        set   = StemLoopFields.AStemWasSet(m_StemClips, clips);

            m_StemClips = clips;

            if (set)
            {
                ReadStems();
            }
        }

        /// <summary>Reads every stem's source file for the loop, and takes it when the stems agree on one.</summary>
        private void ReadStems()
        {
            StemLoopReading reading = StemLoopFields.ReadStems(Sfx.Tracks);

            if (reading.Found)
            {
                StemLoopFields.ApplyToSound(serializedObject, reading);
            }

            m_ReadHelp.text          = reading.Report;
            m_ReadHelp.messageType   = reading.Refused ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
            m_ReadHelp.style.display = DisplayStyle.Flex;
        }

        /// <summary>The rate, filled from the first stem's source file while it is unset, its help box, and the seconds.</summary>
        private void RefreshLoopFields()
        {
            serializedObject.Update();

            SerializedProperty rate = serializedObject.FindProperty(SAMPLE_RATE);
            string             help = StemLoopFields.CheckRate(rate, Sfx.Tracks, true);

            m_RateHelp.text          = help ?? string.Empty;
            m_RateHelp.style.display = help != null ? DisplayStyle.Flex : DisplayStyle.None;

            m_LoopStart.Refresh(rate.intValue);
            m_LoopEnd.Refresh(rate.intValue);
        }

        private void Rebuild()
        {
            ReadIfAStemWasSet();
            RefreshLoopFields();

            IReadOnlyList<IStem>         tracks   = Sfx.Tracks;
            IReadOnlyList<ISfxEventData> authored = Sfx.Events;
            AudioClip                    clip     = tracks != null && tracks.Count > 0 ? tracks[0].Clip : null;
            int                          rate     = Sfx.SampleRate;
            bool                         complete = clip != null && !Sfx.Loop;
            int                          count    = (authored?.Count ?? 0) + (complete ? 1 : 0);

            m_EventNames   = new string[count];
            m_EventSeconds = new double[count];
            m_FiresOnce    = new bool[count];
            m_Fired        = new bool[count];
            m_EventLabels  = new Label[count];

            for (int i = 0; authored != null && i < authored.Count; i++)
            {
                m_EventNames[i]   = authored[i].EventName;
                m_EventSeconds[i] = rate > 0 ? (double)authored[i].EventTriggerTimeInSamples / rate : double.NaN;
                m_FiresOnce[i]    = authored[i].RemoveEventOnceTriggered;
            }

            // The sound's end is its first stem's, in that clip's own samples.
            if (complete)
            {
                m_EventNames[count - 1]   = ISfxReference.SFX_COMPLETE;
                m_EventSeconds[count - 1] = (double)clip.samples / clip.frequency;
                m_FiresOnce[count - 1]    = true;
            }

            m_Events.Clear();

            for (int i = 0; i < count; i++)
            {
                m_EventLabels[i] = new Label();
                m_Events.Add(m_EventLabels[i]);
            }

            m_Play.SetEnabled(clip != null);

            Refresh();
        }

        private void Play()
        {
            IReadOnlyList<IStem> tracks = Sfx.Tracks;
            bool[]               active = new bool[tracks.Count];

            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].Clip == null)
                {
                    Debug.LogWarning($"{nameof(SfxAssetEditor)}::{nameof(Play)} Stem [{i + 1}] of [{target.name}] has no clip, so it cannot be previewed");

                    return;
                }

                active[i] = true;
            }

            for (int i = 0; i < m_Fired.Length; i++)
            {
                m_Fired[i] = false;
            }

            double rate = Sfx.SampleRate;

            m_Preview.Play(tracks, active, 0.0, Sfx.Loop ? Sfx.LoopStart / rate : 0.0, Sfx.Loop ? Sfx.LoopEnd / rate : 0.0);
        }

        private void FireEventsReached(double seconds)
        {
            for (int i = 0; i < m_Fired.Length; i++)
            {
                m_Fired[i] |= seconds >= m_EventSeconds[i];
            }
        }

        // The player raises again, each time round, every event not marked to fire once.
        private void RearmEvents()
        {
            for (int i = 0; i < m_Fired.Length; i++)
            {
                m_Fired[i] &= m_FiresOnce[i];
            }
        }

        // The player raises any event the sound ended before reaching as it completes.
        private void FireRemainingEvents()
        {
            for (int i = 0; i < m_Fired.Length; i++)
            {
                m_Fired[i] = true;
            }
        }

        private void Refresh()
        {
            IReadOnlyList<IStem> tracks = Sfx.Tracks;
            AudioClip            clip   = tracks != null && tracks.Count > 0 ? tracks[0].Clip : null;
            double               rate   = Sfx.SampleRate;
            string               loop   = Sfx.Loop ? $"loops from {Seconds(Sfx.LoopStart / rate)} to {Seconds(Sfx.LoopEnd / rate)}" : "plays through";

            m_Position.text = m_Preview.IsPlaying && clip != null
                                  ? $"{Seconds(m_Preview.Position)} of {Seconds((double)clip.samples / clip.frequency)}, {loop}"
                                  : $"Stopped, {loop}";

            // An event lights up once raised, as the player would have raised it by now.
            for (int i = 0; i < m_EventLabels.Length; i++)
            {
                m_EventLabels[i].text                          = $"{m_EventNames[i]} at {Seconds(m_EventSeconds[i])}";
                m_EventLabels[i].style.opacity                 = m_Fired[i] ? 1f : 0.45f;
                m_EventLabels[i].style.unityFontStyleAndWeight = m_Fired[i] ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private static string Seconds(double seconds)
        {
            return double.IsNaN(seconds) ? "an unknown time, with no sample rate" : $"{seconds:0.000} s";
        }
    }
}
