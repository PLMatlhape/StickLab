using System.Collections.Generic;
using UnityEngine;

namespace StickLab.Utils
{
    public static class VectorUtils
    {
        public static List<Vector2> GenerateCirclePoints(Vector2 center, float radius, int segments)
        {
            segments = Mathf.Max(3, segments);
            List<Vector2> points = new List<Vector2>(segments);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float angle = t * Mathf.PI * 2f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                points.Add(center + offset);
            }

            return points;
        }

        public static List<Vector2> GenerateLinePoints(Vector2 start, Vector2 end, int segments)
        {
            segments = Mathf.Max(2, segments);
            List<Vector2> points = new List<Vector2>(segments);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                points.Add(Vector2.Lerp(start, end, t));
            }

            return points;
        }

        public static List<Vector2> GeneratePolygonPoints(Vector2 center, float radius, int sides, float rotationDegrees = 0f)
        {
            sides = Mathf.Max(3, sides);
            List<Vector2> points = new List<Vector2>(sides);
            float startAngle = rotationDegrees * Mathf.Deg2Rad;

            for (int i = 0; i < sides; i++)
            {
                float angle = startAngle + ((i / (float)sides) * Mathf.PI * 2f);
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            return points;
        }

        public static List<Vector2> GenerateSineCurvePoints(Vector2 start, Vector2 end, float amplitude, int segments, int waves = 1)
        {
            segments = Mathf.Max(2, segments);
            waves = Mathf.Max(1, waves);
            List<Vector2> points = new List<Vector2>(segments);
            Vector2 line = end - start;
            Vector2 lineDirection = line.normalized;
            Vector2 perpendicular = new Vector2(-lineDirection.y, lineDirection.x);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                float offset = Mathf.Sin(t * Mathf.PI * 2f * waves) * amplitude;
                points.Add(start + (line * t) + (perpendicular * offset));
            }

            return points;
        }

        public static float DistanceToPolyline(Vector2 point, IReadOnlyList<Vector2> polyline, bool closedLoop, out Vector2 closestPoint, out int closestSegmentIndex)
        {
            closestPoint = point;
            closestSegmentIndex = -1;

            if (polyline == null || polyline.Count < 2)
            {
                return 0f;
            }

            float shortestDistance = float.MaxValue;
            int segmentCount = closedLoop ? polyline.Count : polyline.Count - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                int nextIndex = (i + 1) % polyline.Count;
                Vector2 segmentStart = polyline[i];
                Vector2 segmentEnd = polyline[nextIndex];
                Vector2 candidate = ProjectPointOnSegment(point, segmentStart, segmentEnd);
                float distance = Vector2.Distance(point, candidate);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPoint = candidate;
                    closestSegmentIndex = i;
                }
            }

            return shortestDistance;
        }

        public static Vector2 ProjectPointOnSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float segmentLengthSquared = segment.sqrMagnitude;

            if (segmentLengthSquared <= Mathf.Epsilon)
            {
                return segmentStart;
            }

            float t = Vector2.Dot(point - segmentStart, segment) / segmentLengthSquared;
            t = Mathf.Clamp01(t);
            return segmentStart + (segment * t);
        }
    }
}
