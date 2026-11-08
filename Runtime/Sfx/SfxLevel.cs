using Unity.Mathematics;

namespace RPGFramework.Audio.Sfx
{
    /// <summary>A level that goes to a new value at once or over time, a sound's volume or pan.</summary>
    internal struct SfxLevel
    {
        private float m_From;
        private float m_To;
        private float m_Elapsed;
        private float m_Seconds;
        private bool  m_Changing;

        internal SfxLevel(float value)
        {
            Current    = value;
            m_From     = value;
            m_To       = value;
            m_Elapsed  = 0f;
            m_Seconds  = 0f;
            m_Changing = false;
        }

        internal float Current { get; private set; }

        /// <summary>Goes to a value from the one it has now, over the given time; none goes at once.</summary>
        internal void MoveTo(float value, float seconds)
        {
            m_From     = Current;
            m_To       = value;
            m_Elapsed  = 0f;
            m_Seconds  = seconds;
            m_Changing = true;

            if (seconds <= 0f)
            {
                Current = value;
            }
        }

        /// <returns>Whether <see cref="Current" /> has something new for the sources.</returns>
        internal bool Tick(float deltaTime)
        {
            if (!m_Changing)
            {
                return false;
            }

            if (m_Seconds > 0f)
            {
                m_Elapsed += deltaTime;

                float through = math.saturate(m_Elapsed / m_Seconds);

                Current = math.lerp(m_From, m_To, through);

                if (through < 1f)
                {
                    return true;
                }
            }

            m_Changing = false;

            return true;
        }
    }
}
