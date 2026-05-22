using UnityEngine;

namespace StickLab.Core
{
    public class CursorController : MonoBehaviour
    {
        [Header("Cursor")]
        [SerializeField] private Transform cursorVisual;
        [SerializeField] private Rect      playArea    = new Rect(-4f, -2.5f, 8f, 5f);
        [SerializeField, Min(0f)] private float sensitivity = 8f;
        [SerializeField] private float fixedZ = 0f;

        [Header("Smoothing")]
        [SerializeField] private bool enableCursorSmoothing  = true;
        [SerializeField, Range(0.01f, 0.5f)] private float smoothAlpha = 0.15f;
        [SerializeField] private bool enableCursorTrailing   = true;
        [SerializeField] private int  trailLength            = 8;

        private CursorSmoother       smoother;
        private CursorTrailRenderer  trailRenderer;

        public Vector2 Position2D { get; private set; }

        private Transform ActiveTransform => cursorVisual != null ? cursorVisual : transform;

        private void Awake()
        {
            Vector3 startPos = ActiveTransform.position;
            Position2D = new Vector2(startPos.x, startPos.y);
            fixedZ     = startPos.z;

            if (enableCursorSmoothing)
            {
                var settings = new CursorSmoother.SmoothSettings
                {
                    smoothAlpha    = smoothAlpha,
                    enableTrailing = enableCursorTrailing,
                    trailLength    = trailLength,
                    trailFade      = 0.8f
                };
                smoother = new CursorSmoother(settings);
                smoother.Reset(Position2D);

                if (enableCursorTrailing)
                {
                    var trailGO = new GameObject("CursorTrail");
                    trailGO.transform.SetParent(transform);
                    trailRenderer = trailGO.AddComponent<CursorTrailRenderer>();
                    trailRenderer.SetSmoother(smoother);
                }
            }
        }

        private void Start() => ApplyToTransform();

        /// <summary>Called every frame by GameLoop.</summary>
        public void Tick(Vector2 stickInput, float deltaTime, float sensitivityMultiplier = 1f)
        {
            float   effSens   = sensitivity * Mathf.Max(0f, sensitivityMultiplier);
            // FIX: clamp BEFORE passing to smoother to avoid double-clamping artefacts
            Vector2 rawNext   = Position2D + stickInput * effSens * deltaTime;
            Vector2 clamped   = ClampToPlayArea(rawNext);

            if (enableCursorSmoothing && smoother != null)
            {
                smoother.Update(clamped);
                // Smooth output is already within play area (input was clamped)
                Position2D = smoother.SmoothedPosition;

                if (enableCursorTrailing && trailRenderer != null)
                    trailRenderer.UpdateTrail();
            }
            else
            {
                Position2D = clamped;
            }

            ApplyToTransform();
        }

        public void SetPosition(Vector2 worldPosition)
        {
            Position2D = ClampToPlayArea(worldPosition);
            smoother?.Reset(Position2D);
            ApplyToTransform();
        }

        public void SetPlayArea(Rect newPlayArea)
        {
            playArea   = newPlayArea;
            Position2D = ClampToPlayArea(Position2D);
            ApplyToTransform();
        }

        public void SetSensitivity(float v)  => sensitivity  = Mathf.Max(0f, v);
        public float GetSensitivity()        => sensitivity;
        public bool  GetCursorSmoothingEnabled() => enableCursorSmoothing;
        public float GetSmoothAlpha()        => smoothAlpha;

        public void SetCursorSmoothing(bool enabled) => enableCursorSmoothing = enabled;

        public void SetSmoothAlpha(float alpha)
        {
            smoothAlpha = Mathf.Clamp(alpha, 0.01f, 0.5f);
            smoother?.SetSettings(new CursorSmoother.SmoothSettings
            {
                smoothAlpha    = smoothAlpha,
                enableTrailing = enableCursorTrailing,
                trailLength    = trailLength,
                trailFade      = 0.8f
            });
        }

        private Vector2 ClampToPlayArea(Vector2 p) =>
            new Vector2(Mathf.Clamp(p.x, playArea.xMin, playArea.xMax),
                        Mathf.Clamp(p.y, playArea.yMin, playArea.yMax));

        private void ApplyToTransform() =>
            ActiveTransform.position = new Vector3(Position2D.x, Position2D.y, fixedZ);
    }
}
