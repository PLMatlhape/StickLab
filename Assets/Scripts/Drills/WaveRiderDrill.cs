using System.Collections.Generic;
using StickLab.Rendering;
using StickLab.Scoring;
using StickLab.Utils;
using UnityEngine;

namespace StickLab.Drills
{
    /// <summary>Sine-wave follow drill. Tests smooth oscillating motion.</summary>
    public class WaveRiderDrill : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private Vector2 center        = Vector2.zero;
        [SerializeField, Min(0.5f)] private float length    = 6.5f;
        [SerializeField, Min(0.05f)] private float amplitude = 0.75f;
        [SerializeField, Min(1)]    private int   waves     = 2;
        [SerializeField, Min(8)]    private int   segments  = 120;
        [SerializeField, Min(0.01f)] private float pathThickness = 0.24f;

        [Header("Scoring")]
        [SerializeField, Min(0.01f)] private float allowableDeviation  = 0.17f;
        [SerializeField, Min(0.01f)] private float smoothnessTolerance = 16f;
        [SerializeField, Min(0.01f)] private float speedTolerance      = 2.0f;
        [SerializeField, Min(0.01f)] private float speedSmoothing      = 0.18f;

        public float AccuracyScore         { get; private set; }
        public float SmoothnessScore       { get; private set; }
        public float SpeedConsistencyScore { get; private set; }
        public float TotalScore            { get; private set; }
        public float CurrentDeviation      { get; private set; }
        public Vector2 StartPosition       => pathPoints.Count > 0 ? pathPoints[0] : center + Vector2.left * length * 0.5f;

        private readonly List<Vector2> pathPoints = new List<Vector2>();
        private Vector2 previousPos, previousVel;
        private float   smoothedSpeed;
        private bool    hasPrev;

        private void Start()      => BeginDrill();
        private void OnValidate() { if (Application.isPlaying) BuildPath(); }

        public void BeginDrill()  { ResetMetrics(); BuildPath(); }

        public void ResetMetrics()
        {
            AccuracyScore = SmoothnessScore = SpeedConsistencyScore = TotalScore = CurrentDeviation = 0f;
            previousPos = StartPosition; previousVel = Vector2.zero;
            smoothedSpeed = 0f; hasPrev = false;
        }

        public void BuildPath()
        {
            pathPoints.Clear();
            Vector2 start = center + Vector2.left  * length * 0.5f;
            Vector2 end   = center + Vector2.right * length * 0.5f;
            pathPoints.AddRange(VectorUtils.GenerateSineCurvePoints(start, end, amplitude, segments, waves));
            if (pathRenderer != null) { pathRenderer.SetPath(pathPoints, false); pathRenderer.SetThickness(pathThickness); }
        }

        public void SetParameters(Vector2 newCenter, float newLength, float newAmplitude, int newWaves, float newThickness, float newTolerance)
        {
            center = newCenter; length = newLength; amplitude = newAmplitude;
            waves = Mathf.Max(1, newWaves); pathThickness = newThickness; allowableDeviation = newTolerance;
            BuildPath();
        }

        public void SetPathRenderer(PathRenderer pr) { pathRenderer = pr; BuildPath(); }

        public void Tick(Vector2 pos, float dt)
        {
            if (dt <= 0f) return;
            Vector2 vel   = hasPrev ? (pos - previousPos) / dt : Vector2.zero;
            float   speed = vel.magnitude;
            CurrentDeviation      = VectorUtils.DistanceToPolyline(pos, pathPoints, false, out _, out _);
            AccuracyScore         = AccuracyCalculator.CalculateScore(CurrentDeviation, allowableDeviation);
            SmoothnessScore       = SmoothnessCalculator.CalculateScore(previousVel, vel, dt, smoothnessTolerance);
            SpeedConsistencyScore = SpeedCalculator.CalculateConsistencyScore(speed, ref smoothedSpeed, speedSmoothing, speedTolerance);
            TotalScore            = AccuracyScore * 0.50f + SmoothnessScore * 0.25f + SpeedConsistencyScore * 0.25f;
            previousPos = pos; previousVel = vel; hasPrev = true;
        }
    }
}
