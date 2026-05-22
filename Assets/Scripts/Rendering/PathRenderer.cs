using System.Collections.Generic;
using UnityEngine;

namespace StickLab.Rendering
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Color lineColor = new Color(0.55f, 0.40f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float thickness = 0.25f;

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
            ConfigureRenderer();
        }

        private void OnValidate()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null) ConfigureRenderer();
        }

        public void SetPath(IReadOnlyList<Vector2> points, bool closedLoop)
        {
            if (lineRenderer == null || points == null || points.Count == 0) return;
            lineRenderer.loop          = closedLoop;
            lineRenderer.positionCount = points.Count;
            float z = transform.position.z;
            for (int i = 0; i < points.Count; i++)
                lineRenderer.SetPosition(i, new Vector3(points[i].x, points[i].y, z));
        }

        public void SetThickness(float t)
        {
            thickness = Mathf.Max(0.01f, t);
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = thickness;
                lineRenderer.endWidth   = thickness;
            }
        }

        public void SetColor(Color c)
        {
            lineColor = c;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = c;
                lineRenderer.endColor   = c;
                // Also update material color if using property block
                if (lineRenderer.sharedMaterial != null)
                    lineRenderer.sharedMaterial.color = c;
            }
        }

        public void Clear()
        {
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }

        private void ConfigureRenderer()
        {
            if (lineRenderer == null) return;

            // FIX: always ensure a valid material — Sprites/Default renders in all
            // camera modes (orthographic 2D) without needing lighting
            if (lineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null)
                    lineRenderer.sharedMaterial = new Material(shader);
            }

            lineRenderer.useWorldSpace  = true;
            lineRenderer.loop           = false;
            lineRenderer.startWidth     = thickness;
            lineRenderer.endWidth       = thickness;
            lineRenderer.startColor     = lineColor;
            lineRenderer.endColor       = lineColor;
            lineRenderer.sortingOrder   = 1; // FIX: ensure path renders above background
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
    }
}
