using System.Collections.Generic;
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
        private readonly AudioStemPreview m_Preview = new AudioStemPreview();

        private Button        m_Play;
        private Label         m_Position;
        private VisualElement m_Events;

        // The asset's events, and the completion the player raises after a sound that does not loop, as it publishes them.
        private Label[]  m_EventLabels;
        private string[] m_EventNames;
        private int[]    m_EventSamples;
        private bool[]   m_FiresOnce;
        private bool[]   m_Fired;

        private ISfxAsset Sfx => (SfxAsset)target;

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

            Rebuild();

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

        private void Rebuild()
        {
            IReadOnlyList<IStem>         tracks   = Sfx.Tracks;
            IReadOnlyList<ISfxEventData> authored = Sfx.Events;
            AudioClip                    clip     = tracks != null && tracks.Count > 0 ? tracks[0].Clip : null;
            bool                         complete = clip != null && !Sfx.Loop;
            int                          count    = (authored?.Count ?? 0) + (complete ? 1 : 0);

            m_EventNames   = new string[count];
            m_EventSamples = new int[count];
            m_FiresOnce    = new bool[count];
            m_Fired        = new bool[count];
            m_EventLabels  = new Label[count];

            for (int i = 0; authored != null && i < authored.Count; i++)
            {
                m_EventNames[i]   = authored[i].EventName;
                m_EventSamples[i] = authored[i].EventTriggerTimeInSamples;
                m_FiresOnce[i]    = authored[i].RemoveEventOnceTriggered;
            }

            if (complete)
            {
                m_EventNames[count - 1]   = SfxReference.SFX_COMPLETE;
                m_EventSamples[count - 1] = clip.samples;
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

            m_Preview.Play(tracks, active, 0, Sfx.Loop ? Sfx.LoopStart : 0, Sfx.Loop ? Sfx.LoopEnd : 0);
        }

        private void FireEventsReached(int position)
        {
            for (int i = 0; i < m_Fired.Length; i++)
            {
                m_Fired[i] |= position >= m_EventSamples[i];
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
            int                  rate   = clip != null ? clip.frequency : 0;
            string               loop   = Sfx.Loop ? $"loops from {Seconds(Sfx.LoopStart, rate)} to {Seconds(Sfx.LoopEnd, rate)}" : "plays through";

            m_Position.text = m_Preview.IsPlaying && clip != null
                                  ? $"{Seconds(m_Preview.Position, rate)} of {Seconds(clip.samples, rate)}, {loop}"
                                  : $"Stopped, {loop}";

            // An event lights up once raised, as the player would have raised it by now.
            for (int i = 0; i < m_EventLabels.Length; i++)
            {
                m_EventLabels[i].text                          = $"{m_EventNames[i]} at {Seconds(m_EventSamples[i], rate)}";
                m_EventLabels[i].style.opacity                 = m_Fired[i] ? 1f : 0.45f;
                m_EventLabels[i].style.unityFontStyleAndWeight = m_Fired[i] ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private static string Seconds(int samples, int rate)
        {
            string seconds = rate > 0 ? $"{(float)samples / rate:0.000} s" : $"sample {samples}";

            return seconds;
        }
    }
}
