using UnityEngine;

namespace StickLab.Core
{
    [RequireComponent(typeof(LineRenderer))]
    public class CursorIndicator : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Min(0.01f)] private float radius = 0.12f;
        [SerializeField, Min(3)] private int segments = 24;

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            ConfigureRenderer();
        }

        public void Configure(Color newColor, float newRadius, int newSegments)
        {
            color = newColor;
            radius = Mathf.Max(0.01f, newRadius);
            segments = Mathf.Max(3, newSegments);
            ConfigureRenderer();
        }

        private void LateUpdate()
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.positionCount = segments;

            Vector3 center = transform.position;
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float angle = t * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                lineRenderer.SetPosition(i, center + offset);
            }
        }

        private void ConfigureRenderer()
        {
            if (lineRenderer == null)
            {
                return;
            }

            if (lineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    lineRenderer.sharedMaterial = new Material(shader);
                }
            }

            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.03f;
            lineRenderer.endWidth = 0.03f;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}
