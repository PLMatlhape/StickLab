using System.Collections.Generic;
using StickLab.Rendering;
using StickLab.Scoring;
using StickLab.Utils;
using UnityEngine;

namespace StickLab.Drills
{
    /// <summary>
    /// Infinity Loop (Lemniscate) drill.
    /// The player traces a figure-8 path. A crossing-point smoothness bonus
    /// rewards clean transitions through the centre.
    /// </summary>
    public class InfinityLoopDrill : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private Vector2 center      = Vector2.zero;
        [SerializeField, Min(0.5f)] private float scale    = 2.2f;   // half-width of each lobe
        [SerializeField, Min(8)]    private int   segments = 128;
        [SerializeField, Min(0.01f)] private float pathThickness = 0.28f;

        [Header("Scoring")]
        [SerializeField, Min(0.01f)] private float allowableDeviation  = 0.20f;
        [SerializeField, Min(0.01f)] private float smoothnessTolerance = 18f;
        [SerializeField, Min(0.01f)] private float speedTolerance      = 2.5f;
        [SerializeField, Min(0.01f)] private float speedSmoothing      = 0.20f;

        [Header("Crossing bonus")]
        [SerializeField, Min(0.01f)] private float crossingRadius = 0.25f;  // world-space radius around centre
        [SerializeField, Min(0f)]    private float crossingBonus  = 5f;     // flat score boost per clean crossing

        // ── Public metrics ───────────────────────────────────────────────────
        public float AccuracyScore          { get; private set; }
        public float SmoothnessScore        { get; private set; }
        public float SpeedConsistencyScore  { get; private set; }
        public float TotalScore             { get; private set; }
        public float CurrentDeviation       { get; private set; }
        public int   CrossingCount          { get; private set; }

        /// Start position is the rightmost point of the right lobe
        public Vector2 StartPosition => center + Vector2.right * scale;

        // ── Private state ────────────────────────────────────────────────────
        private readonly List<Vector2> pathPoints = new List<Vector2>();
        private Vector2 previousCursorPosition;
        private Vector2 previousVelocity;
        private float   smoothedSpeed;
        private bool    hasPreviousSample;
        private bool    wasInCrossingZone;   // debounce crossing detection
        private float   crossingBoostTimer;  // time remaining on current boost
        private const float CrossingBoostDuration = 0.4f;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()   => BeginDrill();
        private void OnValidate()
        {
            if (Application.isPlaying) BuildPath();
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void BeginDrill()
        {
            ResetMetrics();
            BuildPath();
        }

        public void ResetMetrics()
        {
            AccuracyScore         = 0f;
            SmoothnessScore       = 0f;
            SpeedConsistencyScore = 0f;
            TotalScore            = 0f;
            CurrentDeviation      = 0f;
            CrossingCount         = 0;
            previousCursorPosition = StartPosition;
            previousVelocity       = Vector2.zero;
            smoothedSpeed          = 0f;
            hasPreviousSample      = false;
            wasInCrossingZone      = false;
            crossingBoostTimer     = 0f;
        }

        public void BuildPath()
        {
            pathPoints.Clear();
            pathPoints.AddRange(VectorUtils.GenerateLemniscatePoints(center, scale, segments));

            if (pathRenderer != null)
            {
                pathRenderer.SetPath(pathPoints, true);   // closed loop
                pathRenderer.SetThickness(pathThickness);
            }
        }

        public void SetParameters(Vector2 newCenter, float newScale, float newThickness, float newDeviation)
        {
            center             = newCenter;
            scale              = newScale;
            pathThickness      = newThickness;
            allowableDeviation = newDeviation;
            BuildPath();
        }

        public void SetPathRenderer(PathRenderer pr)
        {
            pathRenderer = pr;
            BuildPath();
        }

        /// <summary>Main per-frame update called by TrainingSessionManager.</summary>
        public void Tick(Vector2 cursorPosition, float deltaTime)
        {
            if (deltaTime <= 0f) return;

            // ── Velocity / speed ─────────────────────────────────────────────
            Vector2 velocity = hasPreviousSample
                ? (cursorPosition - previousCursorPosition) / deltaTime
                : Vector2.zero;
            float speed = velocity.magnitude;

            // ── Accuracy: distance to lemniscate polyline ────────────────────
            CurrentDeviation = VectorUtils.DistanceToPolyline(
                cursorPosition, pathPoints, true,
                out _, out _);
            AccuracyScore = AccuracyCalculator.CalculateScore(CurrentDeviation, allowableDeviation);

            // ── Smoothness ───────────────────────────────────────────────────
            SmoothnessScore = SmoothnessCalculator.CalculateScore(
                previousVelocity, velocity, deltaTime, smoothnessTolerance);

            // ── Speed consistency ────────────────────────────────────────────
            SpeedConsistencyScore = SpeedCalculator.CalculateConsistencyScore(
                speed, ref smoothedSpeed, speedSmoothing, speedTolerance);

            // ── Crossing bonus ───────────────────────────────────────────────
            crossingBoostTimer = Mathf.Max(0f, crossingBoostTimer - deltaTime);
            DetectCrossing(cursorPosition);

            // ── Weighted total (with optional crossing micro-boost) ───────────
            float baseScore = (AccuracyScore * 0.50f)
                            + (SmoothnessScore * 0.25f)
                            + (SpeedConsistencyScore * 0.25f);

            float boost = crossingBoostTimer > 0f
                ? crossingBonus * (crossingBoostTimer / CrossingBoostDuration)
                : 0f;

            TotalScore = Mathf.Clamp(baseScore + boost, 0f, 100f);

            // ── Advance state ─────────────────────────────────────────────────
            previousCursorPosition = cursorPosition;
            previousVelocity       = velocity;
            hasPreviousSample      = true;
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private void DetectCrossing(Vector2 pos)
        {
            bool inZone = Vector2.Distance(pos, center) <= crossingRadius;

            if (inZone && !wasInCrossingZone)
            {
                // Only award bonus if player is also close to the path at the crossing
                if (CurrentDeviation <= allowableDeviation * 1.5f)
                {
                    CrossingCount++;
                    crossingBoostTimer = CrossingBoostDuration;
                }
            }
            wasInCrossingZone = inZone;
        }
    }
}
