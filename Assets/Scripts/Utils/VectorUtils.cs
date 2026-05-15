using System.Collections.Generic;
using UnityEngine;

namespace StickLab.Utils
{
    public static class VectorUtils
    {
        // ── Existing generators ───────────────────────────────────────────────

        public static List<Vector2> GenerateCirclePoints(Vector2 center, float radius, int segments)
        {
            segments = Mathf.Max(3, segments);
            var points = new List<Vector2>(segments);
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return points;
        }

        public static List<Vector2> GenerateLinePoints(Vector2 start, Vector2 end, int segments)
        {
            segments = Mathf.Max(2, segments);
            var points = new List<Vector2>(segments);
            for (int i = 0; i < segments; i++)
                points.Add(Vector2.Lerp(start, end, i / (float)(segments - 1)));
            return points;
        }

        public static List<Vector2> GeneratePolygonPoints(Vector2 center, float radius, int sides, float rotationDegrees = 0f)
        {
            sides = Mathf.Max(3, sides);
            var points = new List<Vector2>(sides);
            float start = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < sides; i++)
            {
                float angle = start + (i / (float)sides) * Mathf.PI * 2f;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return points;
        }

        public static List<Vector2> GenerateSineCurvePoints(Vector2 start, Vector2 end, float amplitude, int segments, int waves = 1)
        {
            segments = Mathf.Max(2, segments);
            waves    = Mathf.Max(1, waves);
            var points = new List<Vector2>(segments);
            Vector2 line      = end - start;
            Vector2 lineDir   = line.normalized;
            Vector2 perp      = new Vector2(-lineDir.y, lineDir.x);
            for (int i = 0; i < segments; i++)
            {
                float t      = i / (float)(segments - 1);
                float offset = Mathf.Sin(t * Mathf.PI * 2f * waves) * amplitude;
                points.Add(start + line * t + perp * offset);
            }
            return points;
        }

        // ── NEW: Lemniscate of Bernoulli (Infinity Loop) ──────────────────────
        /// <summary>
        /// Parametric lemniscate: x = a·cos(t)/(1+sin²(t)),  y = a·sin(t)cos(t)/(1+sin²(t))
        /// Produces a smooth ∞ shape centred on <paramref name="center"/>.
        /// </summary>
        public static List<Vector2> GenerateLemniscatePoints(Vector2 center, float scale, int segments)
        {
            segments = Mathf.Max(8, segments);
            var points = new List<Vector2>(segments);
            for (int i = 0; i < segments; i++)
            {
                float t   = (i / (float)segments) * Mathf.PI * 2f;
                float den = 1f + Mathf.Sin(t) * Mathf.Sin(t);
                float x   = scale * Mathf.Cos(t) / den;
                float y   = scale * Mathf.Sin(t) * Mathf.Cos(t) / den;
                points.Add(center + new Vector2(x, y));
            }
            return points;
        }

        // ── NEW: Spiral (Archimedean) ─────────────────────────────────────────
        /// <summary>
        /// Archimedean spiral from <paramref name="innerRadius"/> outward.
        /// Path is open (not closed loop).
        /// </summary>
        public static List<Vector2> GenerateSpiralPoints(Vector2 center, float innerRadius, float outerRadius, float turns, int segments)
        {
            segments = Mathf.Max(8, segments);
            var points = new List<Vector2>(segments);
            for (int i = 0; i < segments; i++)
            {
                float t     = i / (float)(segments - 1);
                float angle = t * turns * Mathf.PI * 2f;
                float r     = Mathf.Lerp(innerRadius, outerRadius, t);
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
            }
            return points;
        }

        // ── NEW: Zig-Zag ─────────────────────────────────────────────────────
        public static List<Vector2> GenerateZigZagPoints(Vector2 start, Vector2 end, float amplitude, int peaks, int samplesPerSegment = 8)
        {
            peaks = Mathf.Max(1, peaks);
            int totalSamples = peaks * 2 * samplesPerSegment + 1;
            var points = new List<Vector2>(totalSamples);
            Vector2 line    = end - start;
            Vector2 lineDir = line.normalized;
            Vector2 perp    = new Vector2(-lineDir.y, lineDir.x);
            int totalPts    = peaks * 2;

            for (int seg = 0; seg < totalPts; seg++)
            {
                float tStart = seg / (float)totalPts;
                float tEnd   = (seg + 1) / (float)totalPts;
                float yStart = (seg % 2 == 0) ? amplitude : -amplitude;
                float yEnd   = (seg % 2 == 0) ? -amplitude : amplitude;
                if (seg == 0) yStart = 0f;
                if (seg == totalPts - 1) yEnd = 0f;

                int count = (seg == totalPts - 1) ? samplesPerSegment + 1 : samplesPerSegment;
                for (int i = (seg == 0 ? 0 : 0); i < count; i++)
                {
                    float t   = i / (float)(samplesPerSegment);
                    float pos = Mathf.Lerp(tStart, tEnd, t);
                    float amp = Mathf.Lerp(yStart, yEnd, t);
                    points.Add(start + line * pos + perp * amp);
                }
            }
            return points;
        }

        // ── Geometry utilities ────────────────────────────────────────────────

        public static float DistanceToPolyline(Vector2 point, IReadOnlyList<Vector2> polyline,
            bool closedLoop, out Vector2 closestPoint, out int closestSegmentIndex)
        {
            closestPoint        = point;
            closestSegmentIndex = -1;
            if (polyline == null || polyline.Count < 2) return 0f;

            float shortest    = float.MaxValue;
            int   segCount    = closedLoop ? polyline.Count : polyline.Count - 1;

            for (int i = 0; i < segCount; i++)
            {
                int     next      = (i + 1) % polyline.Count;
                Vector2 candidate = ProjectPointOnSegment(point, polyline[i], polyline[next]);
                float   dist      = Vector2.Distance(point, candidate);
                if (dist < shortest)
                {
                    shortest            = dist;
                    closestPoint        = candidate;
                    closestSegmentIndex = i;
                }
            }
            return shortest;
        }

        public static Vector2 ProjectPointOnSegment(Vector2 point, Vector2 segStart, Vector2 segEnd)
        {
            Vector2 seg = segEnd - segStart;
            float   sqr = seg.sqrMagnitude;
            if (sqr <= Mathf.Epsilon) return segStart;
            float t = Mathf.Clamp01(Vector2.Dot(point - segStart, seg) / sqr);
            return segStart + seg * t;
        }
    }
}
