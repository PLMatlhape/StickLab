using UnityEngine;

namespace StickLab.Scoring
{
    public static class SpeedCalculator
    {
        public static float CalculateConsistencyScore(float currentSpeed, ref float smoothedSpeed, float smoothingFactor, float tolerance)
        {
            float smoothing = Mathf.Clamp01(smoothingFactor);
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, smoothing);

            float deviation = Mathf.Abs(currentSpeed - smoothedSpeed);
            float normalizedError = Mathf.Clamp01(deviation / Mathf.Max(tolerance, Mathf.Epsilon));
            return (1f - normalizedError) * 100f;
        }
    }
}
