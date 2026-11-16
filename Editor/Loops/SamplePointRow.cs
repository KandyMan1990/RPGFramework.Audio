using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>
    /// A loop point in samples, for a song or a sound, its field labelled as an inspector field is, then the same point in
    /// seconds at the asset's sample rate, typing in either moving the other, and for a song the bar it falls in.
    /// </summary>
    internal sealed class SamplePointRow
    {
        private const float SECONDS_WIDTH = 80f;
        private const float BAR_WIDTH     = 56f;

        private readonly SerializedProperty m_Samples;
        private readonly string             m_RatePath;
        private readonly DoubleField        m_Seconds;
        private readonly Label              m_Bar;

        /// <param name="samples">The point's property, in samples.</param>
        /// <param name="label">The field's label.</param>
        /// <param name="ratePath">The asset's sample rate's property, which the samples are counted at.</param>
        /// <param name="showsBar">Whether the row shows the bar the point falls in, as a song's does.</param>
        internal SamplePointRow(SerializedProperty samples, string label, string ratePath, bool showsBar)
        {
            m_Samples  = samples;
            m_RatePath = ratePath;

            IntegerField field = new IntegerField(label) { tooltip = samples.tooltip };
            field.BindProperty(samples);
            field.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            field.style.flexGrow = 1f;

            m_Seconds = new DoubleField
                        {
                            isDelayed = true,
                            tooltip   = "The same point in seconds, at the sample rate"
                        };
            m_Seconds.style.width = SECONDS_WIDTH;
            m_Seconds.RegisterValueChangedCallback(changed => SetSeconds(changed.newValue));

            Root = new VisualElement();
            Root.style.flexDirection = FlexDirection.Row;
            Root.Add(field);
            Root.Add(m_Seconds);

            if (!showsBar)
            {
                return;
            }

            m_Bar = new Label
                    {
                        tooltip = "The bar it falls in, through the tempo sections"
                    };
            m_Bar.style.width          = BAR_WIDTH;
            m_Bar.style.unityTextAlign = TextAnchor.MiddleLeft;

            Root.Add(m_Bar);
        }

        internal VisualElement Root { get; }

        /// <returns>The point in seconds at <paramref name="rate" />, or 0 with no rate.</returns>
        internal double Refresh(int rate)
        {
            double seconds = rate > 0 ? (double)m_Samples.intValue / rate : 0.0;

            m_Seconds.SetValueWithoutNotify(seconds);
            m_Seconds.SetEnabled(rate > 0);

            return seconds;
        }

        internal void SetBar(string text)
        {
            m_Bar.text = text;
        }

        private void SetSeconds(double seconds)
        {
            int rate = m_Samples.serializedObject.FindProperty(m_RatePath).intValue;

            if (rate <= 0)
            {
                return;
            }

            m_Samples.intValue = (int)Math.Round(seconds * rate);
            m_Samples.serializedObject.ApplyModifiedProperties();
        }
    }
}
