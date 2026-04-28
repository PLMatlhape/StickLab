using UnityEngine;

namespace StickLab.Core
{
    /// <summary>
    /// Smooths cursor movement with interpolation and optional trailing/ghosting.
    /// Provides COD-style smooth tracking without jitter.
    /// </summary>
    public class CursorSmoother
    {
        [System.Serializable]
        public struct SmoothSettings
        {
            [Range(0f, 1f)]
            public float smoothAlpha = 0.15f;  // Higher = more smoothing, lower = more responsive
            public bool enableTrailing = true;
            public int trailLength = 8;
            public float trailFade = 0.8f;
        }

        private Vector2 smoothedPosition;
        private Vector2 lastRawPosition;
        private SmoothSettings settings;
        private Vector2[] trailPositions;
        private int trailIndex;
        private bool isInitialized;

        public Vector2 SmoothedPosition => smoothedPosition;
        public Vector2[] TrailPositions => trailPositions;
        public int TrailCount => settings.enableTrailing ? settings.trailLength : 0;

        public CursorSmoother(SmoothSettings smoothSettings)
        {
            settings = smoothSettings;
            smoothedPosition = Vector2.zero;
            lastRawPosition = Vector2.zero;
            
            if (settings.enableTrailing)
            {
                trailPositions = new Vector2[settings.trailLength];
                trailIndex = 0;
            }

            isInitialized = false;
        }

        public void Update(Vector2 rawPosition)
        {
            // First frame initialization
            if (!isInitialized)
            {
                smoothedPosition = rawPosition;
                lastRawPosition = rawPosition;
                isInitialized = true;

                if (settings.enableTrailing)
                {
                    for (int i = 0; i < trailPositions.Length; i++)
                    {
                        trailPositions[i] = rawPosition;
                    }
                }

                return;
            }

            // Exponential moving average for smoothing
            smoothedPosition = Vector2.Lerp(smoothedPosition, rawPosition, settings.smoothAlpha);

            // Update trail
            if (settings.enableTrailing && Vector2.Distance(smoothedPosition, lastRawPosition) > 0.05f)
            {
                trailPositions[trailIndex] = smoothedPosition;
                trailIndex = (trailIndex + 1) % settings.trailLength;
                lastRawPosition = smoothedPosition;
            }
        }

        public void SetSettings(SmoothSettings newSettings)
        {
            settings = newSettings;

            if (settings.enableTrailing && trailPositions == null)
            {
                trailPositions = new Vector2[settings.trailLength];
                for (int i = 0; i < trailPositions.Length; i++)
                {
                    trailPositions[i] = smoothedPosition;
                }
            }
        }

        public void Reset(Vector2 initialPosition)
        {
            smoothedPosition = initialPosition;
            lastRawPosition = initialPosition;
            isInitialized = true;

            if (settings.enableTrailing)
            {
                for (int i = 0; i < trailPositions.Length; i++)
                {
                    trailPositions[i] = initialPosition;
                }
            }
        }
    }

    /// <summary>
    /// Renders cursor trailing effects using line renderer.
    /// Shows the smoothed path the cursor took (like in FPS games).
    /// </summary>
    public class CursorTrailRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer trailRenderer;
        [SerializeField] private Color trailColor = new Color(0.57f, 0.42f, 1f, 0.5f);
        [SerializeField] private float trailThickness = 0.04f;
        
        private CursorSmoother smoother;

        private void Awake()
        {
            if (trailRenderer == null)
            {
                trailRenderer = GetComponent<LineRenderer>();
                if (trailRenderer == null)
                {
                    trailRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            ConfigureRenderer();
        }

        private void ConfigureRenderer()
        {
            if (trailRenderer == null) return;

            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.startWidth = trailThickness;
            trailRenderer.endWidth = trailThickness;
            trailRenderer.startColor = trailColor;
            trailRenderer.endColor = trailColor;
            trailRenderer.sortingOrder = 10;
        }

        public void SetSmoother(CursorSmoother smootherRef)
        {
            smoother = smootherRef;
        }

        public void UpdateTrail()
        {
            if (smoother == null || trailRenderer == null) return;

            Vector2[] trail = smoother.TrailPositions;
            int trailCount = smoother.TrailCount;

            if (trailCount == 0)
            {
                trailRenderer.positionCount = 0;
                return;
            }

            trailRenderer.positionCount = trailCount;

            for (int i = 0; i < trailCount; i++)
            {
                Vector3 pos = new Vector3(trail[i].x, trail[i].y, 0f);
                trailRenderer.SetPosition(i, pos);
            }
        }

        public void SetTrailColor(Color newColor)
        {
            trailColor = newColor;
            if (trailRenderer != null)
            {
                trailRenderer.startColor = trailColor;
                trailRenderer.endColor = trailColor;
            }
        }

        public void SetTrailThickness(float thickness)
        {
            trailThickness = Mathf.Max(0.01f, thickness);
            if (trailRenderer != null)
            {
                trailRenderer.startWidth = trailThickness;
                trailRenderer.endWidth = trailThickness;
            }
        }

        public void Clear()
        {
            if (trailRenderer != null)
            {
                trailRenderer.positionCount = 0;
            }
        }
    }
}
