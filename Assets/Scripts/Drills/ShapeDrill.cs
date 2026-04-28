using System.Collections.Generic;
using StickLab.Rendering;
using StickLab.Scoring;
using StickLab.Utils;
using UnityEngine;

namespace StickLab.Drills
{
    public class ShapeDrill : MonoBehaviour
    {
        public enum ShapeType
        {
            Triangle,
            Square,
            Pentagon,
            Circle
        }

        [Header("Path")]
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private ShapeType shapeType = ShapeType.Triangle;
        [SerializeField] private Vector2 center = Vector2.zero;
        [SerializeField, Min(0.1f)] private float radius = 2f;
        [SerializeField, Min(3)] private int segmentCount = 64;
        [SerializeField, Min(0.01f)] private float pathThickness = 0.25f;
        [SerializeField, Range(0f, 360f)] private float rotationDegrees = 0f;

        [Header("Scoring")]
        [SerializeField, Min(0.01f)] private float allowableDeviation = 0.18f;
        [SerializeField, Min(0.01f)] private float smoothnessTolerance = 16f;
        [SerializeField, Min(0.01f)] private float speedTolerance = 2.25f;
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

            switch (shapeType)
            {
                case ShapeType.Triangle:
                    pathPoints.AddRange(VectorUtils.GeneratePolygonPoints(center, radius, 3, rotationDegrees));
                    break;
                case ShapeType.Square:
                    pathPoints.AddRange(VectorUtils.GeneratePolygonPoints(center, radius, 4, rotationDegrees + 45f));
                    break;
                case ShapeType.Pentagon:
                    pathPoints.AddRange(VectorUtils.GeneratePolygonPoints(center, radius, 5, rotationDegrees));
                    break;
                case ShapeType.Circle:
                    pathPoints.AddRange(VectorUtils.GenerateCirclePoints(center, radius, segmentCount));
                    break;
            }

            if (pathRenderer != null)
            {
                pathRenderer.SetPath(pathPoints, true);
                pathRenderer.SetThickness(pathThickness);
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

        public void SetParameters(ShapeType newShapeType, float newRadius, float newThickness, float newAllowableDeviation, Vector2 newCenter)
        {
            shapeType = newShapeType;
            radius = newRadius;
            pathThickness = newThickness;
            allowableDeviation = newAllowableDeviation;
            center = newCenter;
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
            CurrentDeviation = VectorUtils.DistanceToPolyline(cursorPosition, pathPoints, true, out closestPoint, out segmentIndex);
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
    }
}
