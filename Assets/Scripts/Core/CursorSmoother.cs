using UnityEngine;

namespace StickLab.Core
{
    /// <summary>
    /// Smooths cursor movement with exponential averaging and optional trail.
    /// </summary>
    public class CursorSmoother
    {
        [System.Serializable]
        public struct SmoothSettings
        {
            public float smoothAlpha;
            public bool  enableTrailing;
            public int   trailLength;
            public float trailFade;

            // FIX: C#9 structs cannot use inline field defaults.
            // Explicit constructor with defaults — compiles on Unity 2022.3 (C# 9).
            public SmoothSettings(
                float smoothAlpha    = 0.15f,
                bool  enableTrailing = true,
                int   trailLength    = 8,
                float trailFade      = 0.8f)
            {
                this.smoothAlpha    = smoothAlpha;
                this.enableTrailing = enableTrailing;
                this.trailLength    = trailLength;
                this.trailFade      = trailFade;
            }

            public static SmoothSettings Default => new SmoothSettings();
        }

        // ── State ─────────────────────────────────────────────────────────────
        private Vector2  smoothedPosition;
        private Vector2  lastRawPosition;
        private SmoothSettings settings;
        private Vector2[] trailPositions;
        private int      trailIndex;
        private bool     initialized;

        public Vector2   SmoothedPosition => smoothedPosition;
        public Vector2[] TrailPositions   => trailPositions;
        public int       TrailCount       => settings.enableTrailing ? settings.trailLength : 0;

        // ── Constructor ───────────────────────────────────────────────────────
        public CursorSmoother(SmoothSettings s)
        {
            settings = s;
            if (settings.enableTrailing)
            {
                trailPositions = new Vector2[Mathf.Max(1, settings.trailLength)];
                trailIndex     = 0;
            }
            initialized = false;
        }

        // ── Update ────────────────────────────────────────────────────────────
        public void Update(Vector2 rawPosition)
        {
            if (!initialized)
            {
                smoothedPosition = rawPosition;
                lastRawPosition  = rawPosition;
                initialized      = true;
                if (settings.enableTrailing && trailPositions != null)
                    for (int i = 0; i < trailPositions.Length; i++)
                        trailPositions[i] = rawPosition;
                return;
            }

            // Exponential moving average
            smoothedPosition = Vector2.Lerp(smoothedPosition, rawPosition, settings.smoothAlpha);

            // Trail — only record when position moves enough to avoid spam
            if (settings.enableTrailing && trailPositions != null
                && Vector2.Distance(smoothedPosition, lastRawPosition) > 0.05f)
            {
                trailPositions[trailIndex] = smoothedPosition;
                trailIndex = (trailIndex + 1) % trailPositions.Length;
                lastRawPosition = smoothedPosition;
            }
        }

        public void SetSettings(SmoothSettings newSettings)
        {
            settings = newSettings;
            if (settings.enableTrailing && trailPositions == null)
            {
                trailPositions = new Vector2[Mathf.Max(1, settings.trailLength)];
                for (int i = 0; i < trailPositions.Length; i++)
                    trailPositions[i] = smoothedPosition;
            }
        }

        public void Reset(Vector2 position)
        {
            smoothedPosition = position;
            lastRawPosition  = position;
            initialized      = true;
            if (settings.enableTrailing && trailPositions != null)
                for (int i = 0; i < trailPositions.Length; i++)
                    trailPositions[i] = position;
        }
    }

    /// <summary>Renders cursor trail using a LineRenderer.</summary>
    public class CursorTrailRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer trailRenderer;
        [SerializeField] private Color  trailColor     = new Color(0.57f, 0.42f, 1f, 0.5f);
        [SerializeField] private float  trailThickness = 0.04f;

        private CursorSmoother smoother;

        private void Awake()
        {
            if (trailRenderer == null)
                trailRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
            ConfigureRenderer();
        }

        private void ConfigureRenderer()
        {
            if (trailRenderer == null) return;
            if (trailRenderer.sharedMaterial == null)
                trailRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.startWidth  = trailThickness;
            trailRenderer.endWidth    = trailThickness;
            trailRenderer.startColor  = trailColor;
            trailRenderer.endColor    = trailColor;
            trailRenderer.sortingOrder = 10;
        }

        public void SetSmoother(CursorSmoother s) => smoother = s;

        public void UpdateTrail()
        {
            if (smoother == null || trailRenderer == null) return;
            Vector2[] trail = smoother.TrailPositions;
            int count = smoother.TrailCount;
            if (count == 0 || trail == null) { trailRenderer.positionCount = 0; return; }
            trailRenderer.positionCount = count;
            for (int i = 0; i < count; i++)
                trailRenderer.SetPosition(i, new Vector3(trail[i].x, trail[i].y, 0f));
        }

        public void SetTrailColor(Color c)
        {
            trailColor = c;
            if (trailRenderer != null) { trailRenderer.startColor = c; trailRenderer.endColor = c; }
        }

        public void SetTrailThickness(float t)
        {
            trailThickness = Mathf.Max(0.01f, t);
            if (trailRenderer != null) { trailRenderer.startWidth = trailThickness; trailRenderer.endWidth = trailThickness; }
        }

        public void Clear() { if (trailRenderer != null) trailRenderer.positionCount = 0; }
    }
}
