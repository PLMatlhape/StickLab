using System.Collections.Generic;
using StickLab.Rendering;
using StickLab.Scoring;
using StickLab.Utils;
using UnityEngine;

namespace StickLab.Drills
{
    /// <summary>
    /// Archimedean spiral drill.
    /// Path winds inward; difficulty increases as the path narrows.
    /// </summary>
    public class SpiralDrill : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private Vector2 center       = Vector2.zero;
        [SerializeField, Min(0.1f)] private float innerRadius = 0.4f;
        [SerializeField, Min(0.2f)] private float outerRadius = 2.4f;
        [SerializeField, Min(0.5f)] private float turns        = 2.5f;
        [SerializeField, Min(8)]    private int   segments     = 160;
        [SerializeField, Min(0.01f)] private float pathThickness = 0.25f;

        [Header("Scoring")]
        [SerializeField, Min(0.01f)] private float allowableDeviation  = 0.18f;
        [SerializeField, Min(0.01f)] private float smoothnessTolerance = 16f;
        [SerializeField, Min(0.01f)] private float speedTolerance      = 2.25f;
        [SerializeField, Min(0.01f)] private float speedSmoothing      = 0.20f;

        // ── Public metrics ────────────────────────────────────────────────────
        public float AccuracyScore         { get; private set; }
        public float SmoothnessScore       { get; private set; }
        public float SpeedConsistencyScore { get; private set; }
        public float TotalScore            { get; private set; }
        public float CurrentDeviation      { get; private set; }

        public Vector2 StartPosition => pathPoints.Count > 0 ? pathPoints[0] : center + Vector2.right * outerRadius;

        // ── Private state ─────────────────────────────────────────────────────
        private readonly List<Vector2> pathPoints = new List<Vector2>();
        private Vector2 previousCursorPosition;
        private Vector2 previousVelocity;
        private float   smoothedSpeed;
        private bool    hasPreviousSample;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()      => BeginDrill();
        private void OnValidate() { if (Application.isPlaying) BuildPath(); }

        // ── Public API ────────────────────────────────────────────────────────
        public void BeginDrill()
        {
            ResetMetrics();
            BuildPath();
        }

        public void ResetMetrics()
        {
            AccuracyScore = SmoothnessScore = SpeedConsistencyScore = TotalScore = CurrentDeviation = 0f;
            previousCursorPosition = StartPosition;
            previousVelocity       = Vector2.zero;
            smoothedSpeed          = 0f;
            hasPreviousSample      = false;
        }

        public void BuildPath()
        {
            pathPoints.Clear();
            pathPoints.AddRange(VectorUtils.GenerateSpiralPoints(center, innerRadius, outerRadius, turns, segments));

            if (pathRenderer != null)
            {
                pathRenderer.SetPath(pathPoints, false);  // open path
                pathRenderer.SetThickness(pathThickness);
            }
        }

        public void SetParameters(Vector2 newCenter, float newOuter, float newThickness, float newDeviation)
        {
            center             = newCenter;
            outerRadius        = newOuter;
            pathThickness      = newThickness;
            allowableDeviation = newDeviation;
            BuildPath();
        }

        public void SetPathRenderer(PathRenderer pr) { pathRenderer = pr; BuildPath(); }

        public void Tick(Vector2 cursorPosition, float deltaTime)
        {
            if (deltaTime <= 0f) return;

            Vector2 velocity = hasPreviousSample
                ? (cursorPosition - previousCursorPosition) / deltaTime
                : Vector2.zero;
            float speed = velocity.magnitude;

            CurrentDeviation = VectorUtils.DistanceToPolyline(
                cursorPosition, pathPoints, false, out _, out _);
            AccuracyScore         = AccuracyCalculator.CalculateScore(CurrentDeviation, allowableDeviation);
            SmoothnessScore       = SmoothnessCalculator.CalculateScore(previousVelocity, velocity, deltaTime, smoothnessTolerance);
            SpeedConsistencyScore = SpeedCalculator.CalculateConsistencyScore(speed, ref smoothedSpeed, speedSmoothing, speedTolerance);
            TotalScore            = AccuracyScore * 0.50f + SmoothnessScore * 0.25f + SpeedConsistencyScore * 0.25f;

            previousCursorPosition = cursorPosition;
            previousVelocity       = velocity;
            hasPreviousSample      = true;
        }
    }
}
