using System.Collections.Generic;
using StickLab.Rendering;
using StickLab.Scoring;
using StickLab.Utils;
using UnityEngine;

namespace StickLab.Drills
{
    public class CircleDrill : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private Vector2 center = Vector2.zero;
        [SerializeField, Min(0.1f)] private float radius = 2.5f;
        [SerializeField, Min(3)] private int segmentCount = 96;
        [SerializeField, Min(0.01f)] private float pathThickness = 0.35f;

        [Header("Scoring")]
        [SerializeField, Min(0.01f)] private float allowableDeviation = 0.20f;
        [SerializeField, Min(0.01f)] private float smoothnessTolerance = 18f;
        [SerializeField, Min(0.01f)] private float speedTolerance = 2.5f;
        [SerializeField, Min(0.01f)] private float speedSmoothing = 0.20f;

        public Vector2 StartPosition => center + (Vector2.right * radius);
        public float AccuracyScore { get; private set; }
        public float SmoothnessScore { get; private set; }
        public float SpeedConsistencyScore { get; private set; }
        public float TotalScore { get; private set; }
        public float CurrentDeviation { get; private set; }

        private readonly List<Vector2> pathPoints = new List<Vector2>();
        private Vector2 previousCursorPosition;
        private Vector2 previousVelocity;
        private float smoothedSpeed;
        private bool hasPreviousSample;

        private void Start()
        {
            BeginDrill();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                BuildPath();
            }
        }

        public void BeginDrill()
        {
            ResetMetrics();
            BuildPath();
        }

        public void ResetMetrics()
        {
            AccuracyScore = 0f;
            SmoothnessScore = 0f;
            SpeedConsistencyScore = 0f;
            TotalScore = 0f;
            CurrentDeviation = 0f;
            previousCursorPosition = StartPosition;
            previousVelocity = Vector2.zero;
            smoothedSpeed = 0f;
            hasPreviousSample = false;
        }

        public void BuildPath()
        {
            pathPoints.Clear();
            pathPoints.AddRange(VectorUtils.GenerateCirclePoints(center, radius, segmentCount));

            if (pathRenderer != null)
            {
                pathRenderer.SetPath(pathPoints, true);
                pathRenderer.SetThickness(pathThickness);
            }
        }

        public void SetParameters(Vector2 newCenter, float newRadius, float newThickness, float newAllowableDeviation)
        {
            center = newCenter;
            radius = newRadius;
            pathThickness = newThickness;
            allowableDeviation = newAllowableDeviation;
            BuildPath();
        }

        public void SetPathRenderer(PathRenderer newPathRenderer)
        {
            pathRenderer = newPathRenderer;
            BuildPath();
        }

        public void Tick(Vector2 cursorPosition, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector2 velocity = hasPreviousSample ? (cursorPosition - previousCursorPosition) / deltaTime : Vector2.zero;
            float speed = velocity.magnitude;

            CurrentDeviation = Mathf.Abs(Vector2.Distance(center, cursorPosition) - radius);
            AccuracyScore = AccuracyCalculator.CalculateScore(CurrentDeviation, allowableDeviation);
            SmoothnessScore = SmoothnessCalculator.CalculateScore(previousVelocity, velocity, deltaTime, smoothnessTolerance);
            SpeedConsistencyScore = SpeedCalculator.CalculateConsistencyScore(speed, ref smoothedSpeed, speedSmoothing, speedTolerance);
            TotalScore = (AccuracyScore * 0.50f) + (SmoothnessScore * 0.25f) + (SpeedConsistencyScore * 0.25f);

            previousCursorPosition = cursorPosition;
            previousVelocity = velocity;
            hasPreviousSample = true;
        }
    }
}
