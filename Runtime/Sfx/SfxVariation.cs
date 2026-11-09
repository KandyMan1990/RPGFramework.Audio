using Unity.Mathematics;

namespace RPGFramework.Audio.Sfx
{
    /// <summary>Which of a collection's sounds plays next, and at what pitch, from random numbers it is given.</summary>
    internal static class SfxVariation
    {
        internal const int NONE = -1;

        /// <summary>One of <paramref name="count" /> sounds, never <paramref name="last" /> while there is another to pick.</summary>
        /// <param name="count">How many there are.</param>
        /// <param name="last">The one picked last time, or <see cref="NONE" />.</param>
        /// <param name="roll">A random number, 0 to 1.</param>
        internal static int PickIndex(int count, int last, float roll)
        {
            bool avoidsLast = count > 1 && last != NONE && last < count;
            int  options    = avoidsLast ? count - 1 : count;
            int  index      = math.min((int)(roll * options), options - 1);

            return avoidsLast && index >= last ? index + 1 : index;
        }

        /// <summary>The one a collection playing in order plays: at a position, going round, or the one after the last.</summary>
        /// <param name="count">How many there are.</param>
        /// <param name="last">The one played last time, or <see cref="NONE" />.</param>
        /// <param name="position">Counted from 0, or <see cref="ISfxPlayer.NEXT_IN_ORDER" />.</param>
        internal static int InOrderIndex(int count, int last, int position)
        {
            return position == ISfxPlayer.NEXT_IN_ORDER ? (last + 1) % count : position % count;
        }

        /// <param name="variation">How far either way from 1 it can go.</param>
        /// <param name="roll">A random number, 0 to 1.</param>
        internal static float PickPitch(float variation, float roll)
        {
            return 1f + (roll * 2f - 1f) * variation;
        }
    }
}
