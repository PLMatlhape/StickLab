using System.Collections.Generic;
using StickLab.Rendering;
using StickLab.Scoring;
using StickLab.Utils;
using UnityEngine;

namespace StickLab.Drills
{
    public class LineDrill : MonoBehaviour
    {
        public enum LineOrientation
        {
            Horizontal,
            Vertical,
            DiagonalUp,
            DiagonalDown,
            Custom
        }

        [Header("Path")]
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private LineOrientation orientation = LineOrientation.Horizontal;
        [SerializeField] private Vector2 lineCenter = Vector2.zero;
        [SerializeField, Min(0.1f)] private float lineLength = 6f;
        [SerializeField, Min(3)] private int segmentCount = 24;
        [SerializeField, Min(0.01f)] private float lineThickness = 0.25f;
        [SerializeField] private Vector2 customDirection = Vector2.right;

        [Header("Scoring")]
        [SerializeField, Min(0.01f)] private float allowableDeviation = 0.15f;
        [SerializeField, Min(0.01f)] private float smoothnessTolerance = 16f;
        [SerializeField, Min(0.01f)] private float speedTolerance = 2.25f;
        [SerializeField, Min(0.01f)] private float speedSmoothing = 0.20f;

        public Vector2 StartPosition => lineCenter;
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
            BuildPath();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                BuildPath();
            }
        }

        public void BuildPath()
        {
            pathPoints.Clear();

            Vector2 direction = ResolveDirection();
            Vector2 halfVector = direction.normalized * (lineLength * 0.5f);
            Vector2 start = lineCenter - halfVector;
            Vector2 end = lineCenter + halfVector;

            pathPoints.AddRange(VectorUtils.GenerateLinePoints(start, end, segmentCount));

            if (pathRenderer != null)
            {
                pathRenderer.SetPath(pathPoints, false);
                pathRenderer.SetThickness(lineThickness);
            }
        }

        public void SetPathRenderer(PathRenderer newPathRenderer)
        {
            pathRenderer = newPathRenderer;
            BuildPath();
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

        public void SetParameters(LineOrientation newOrientation, float newLineLength, float newLineThickness, float newAllowableDeviation, Vector2 newCenter)
        {
            orientation = newOrientation;
            lineLength = newLineLength;
            lineThickness = newLineThickness;
            allowableDeviation = newAllowableDeviation;
            lineCenter = newCenter;
            BuildPath();
        }

        public float Evaluate(Vector2 cursorPosition)
        {
            if (pathPoints.Count < 2)
            {
                return 0f;
            }

            Vector2 closestPoint;
            int segmentIndex;
            CurrentDeviation = VectorUtils.DistanceToPolyline(cursorPosition, pathPoints, false, out closestPoint, out segmentIndex);
            return AccuracyCalculator.CalculateScore(CurrentDeviation, allowableDeviation);
        }

        public void Tick(Vector2 cursorPosition, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector2 velocity = hasPreviousSample ? (cursorPosition - previousCursorPosition) / deltaTime : Vector2.zero;
            float speed = velocity.magnitude;

            AccuracyScore = Evaluate(cursorPosition);
            SmoothnessScore = SmoothnessCalculator.CalculateScore(previousVelocity, velocity, deltaTime, smoothnessTolerance);
            SpeedConsistencyScore = SpeedCalculator.CalculateConsistencyScore(speed, ref smoothedSpeed, speedSmoothing, speedTolerance);
            TotalScore = (AccuracyScore * 0.50f) + (SmoothnessScore * 0.25f) + (SpeedConsistencyScore * 0.25f);

            previousCursorPosition = cursorPosition;
            previousVelocity = velocity;
            hasPreviousSample = true;
        }

        private Vector2 ResolveDirection()
        {
            return orientation switch
            {
                LineOrientation.Horizontal => Vector2.right,
                LineOrientation.Vertical => Vector2.up,
                LineOrientation.DiagonalUp => new Vector2(1f, 1f),
                LineOrientation.DiagonalDown => new Vector2(1f, -1f),
                LineOrientation.Custom => customDirection.sqrMagnitude <= Mathf.Epsilon ? Vector2.right : customDirection,
                _ => Vector2.right
            };
        }
    }
}
