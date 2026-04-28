using System.Collections.Generic;
using UnityEngine;

namespace StickLab.Rendering
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField, Min(0.01f)] private float thickness = 0.08f;

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            ConfigureRenderer();
        }

        private void OnValidate()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (lineRenderer != null)
            {
                ConfigureRenderer();
            }
        }

        public void SetPath(IReadOnlyList<Vector2> points, bool closedLoop)
        {
            if (lineRenderer == null || points == null || points.Count == 0)
            {
                return;
            }

            lineRenderer.loop = closedLoop;
            lineRenderer.positionCount = points.Count;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];
                lineRenderer.SetPosition(i, new Vector3(point.x, point.y, transform.position.z));
            }
        }

        public void SetThickness(float newThickness)
        {
            thickness = Mathf.Max(0.01f, newThickness);
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = thickness;
                lineRenderer.endWidth = thickness;
            }
        }

        public void SetColor(Color newColor)
        {
            lineColor = newColor;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
            }
        }

        public void Clear()
        {
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }

        private void ConfigureRenderer()
        {
            if (lineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    lineRenderer.sharedMaterial = new Material(shader);
                }
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.startWidth = thickness;
            lineRenderer.endWidth = thickness;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }
    }
}
