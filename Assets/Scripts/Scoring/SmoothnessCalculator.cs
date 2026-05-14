using UnityEngine;

namespace StickLab.Scoring
{
    public static class SmoothnessCalculator
    {
        public static float CalculateScore(Vector2 previousVelocity, Vector2 currentVelocity, float deltaTime, float tolerance)
        {
            if (deltaTime <= 0f)
            {
                return 100f;
            }

            if (previousVelocity.sqrMagnitude <= Mathf.Epsilon)
            {
                return 100f;
            }

            Vector2 acceleration = (currentVelocity - previousVelocity) / deltaTime;
            float accelerationMagnitude = acceleration.magnitude;
            float normalizedError = Mathf.Clamp01(accelerationMagnitude / Mathf.Max(tolerance, Mathf.Epsilon));
            return (1f - normalizedError) * 100f;
        }
    }
}
