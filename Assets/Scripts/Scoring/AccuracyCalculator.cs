using UnityEngine;

namespace StickLab.Scoring
{
    public static class AccuracyCalculator
    {
        public static float CalculateScore(float deviation, float tolerance)
        {
            if (tolerance <= Mathf.Epsilon)
            {
                return deviation <= Mathf.Epsilon ? 100f : 0f;
            }

            float normalizedError = Mathf.Clamp01(deviation / tolerance);
            return (1f - normalizedError) * 100f;
        }

        public static float CalculateNormalizedError(float deviation, float tolerance)
        {
            if (tolerance <= Mathf.Epsilon)
            {
                return deviation <= Mathf.Epsilon ? 0f : 1f;
            }

            return Mathf.Clamp01(deviation / tolerance);
        }
    }
}
