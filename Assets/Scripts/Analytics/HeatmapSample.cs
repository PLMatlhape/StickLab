using UnityEngine;

namespace StickLab.Analytics
{
    [System.Serializable]
    public struct HeatmapSample
    {
        public Vector2 Position;
        public float Timestamp;
        public float Score;
        public float Velocity;
        public float Intensity;
    }

    [System.Serializable]
    public struct HeatmapSummary
    {
        public int SampleCount;
        public float DurationSeconds;
        public float AverageScore;
        public float PeakScore;
        public float AverageVelocity;
        public Vector2 MeanPosition;
    }
}
