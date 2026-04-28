using System.Collections.Generic;
using UnityEngine;

namespace StickLab.Analytics
{
    public class HeatmapRecorder : MonoBehaviour
    {
        [Header("Recording")]
        [SerializeField, Min(0.005f)] private float sampleInterval = 0.02f;
        [SerializeField, Min(0.1f)] private float stampRadius = 14f;
        [SerializeField, Min(0.1f)] private float stampStrength = 1f;
        [SerializeField] private Rect playArea = new Rect(-4f, -2.5f, 8f, 5f);

        [Header("Render")]
        [SerializeField, Min(64)] private int textureResolution = 256;
        [SerializeField] private Color coldColor = new Color(0.15f, 0.30f, 1f, 0.0f);
        [SerializeField] private Color midColor = new Color(0.57f, 0.42f, 1f, 0.45f);
        [SerializeField] private Color hotColor = new Color(1f, 0.45f, 0.20f, 0.95f);

        public bool IsRecording { get; private set; }
        public IReadOnlyList<HeatmapSample> Samples => samples;
        public Texture2D HeatmapTexture => cachedTexture;

        private readonly List<HeatmapSample> samples = new List<HeatmapSample>(4096);
        private float recordClock;
        private float sampleClock;
        private Vector2 lastPosition;
        private bool hasLastPosition;
        private Texture2D cachedTexture;
        private bool textureDirty = true;

        public void SetPlayArea(Rect newPlayArea)
        {
            playArea = newPlayArea;
            textureDirty = true;
        }

        public void BeginRecording()
        {
            samples.Clear();
            recordClock = 0f;
            sampleClock = 0f;
            hasLastPosition = false;
            IsRecording = true;
            textureDirty = true;
        }

        public void EndRecording()
        {
            IsRecording = false;
        }

        public void Clear()
        {
            samples.Clear();
            recordClock = 0f;
            sampleClock = 0f;
            hasLastPosition = false;
            IsRecording = false;
            textureDirty = true;

            if (cachedTexture != null)
            {
                ClearTexture(cachedTexture);
            }
        }

        public void CaptureSample(Vector2 cursorPosition, float score, float deltaTime)
        {
            if (!IsRecording || deltaTime <= 0f)
            {
                return;
            }

            recordClock += deltaTime;
            sampleClock += deltaTime;

            if (sampleClock < sampleInterval && samples.Count > 0)
            {
                return;
            }

            sampleClock = 0f;

            float velocity = hasLastPosition ? Vector2.Distance(cursorPosition, lastPosition) / Mathf.Max(deltaTime, Mathf.Epsilon) : 0f;
            float intensity = Mathf.Clamp01(score / 100f);

            samples.Add(new HeatmapSample
            {
                Position = cursorPosition,
                Timestamp = recordClock,
                Score = score,
                Velocity = velocity,
                Intensity = intensity
            });

            lastPosition = cursorPosition;
            hasLastPosition = true;
            textureDirty = true;
        }

        public HeatmapSummary GetSummary()
        {
            HeatmapSummary summary = new HeatmapSummary();
            summary.SampleCount = samples.Count;

            if (samples.Count == 0)
            {
                return summary;
            }

            float totalScore = 0f;
            float totalVelocity = 0f;
            float peakScore = 0f;
            Vector2 sum = Vector2.zero;
            float lastTime = samples[samples.Count - 1].Timestamp;

            for (int i = 0; i < samples.Count; i++)
            {
                HeatmapSample sample = samples[i];
                totalScore += sample.Score;
                totalVelocity += sample.Velocity;
                peakScore = Mathf.Max(peakScore, sample.Score);
                sum += sample.Position;
            }

            summary.DurationSeconds = lastTime;
            summary.AverageScore = totalScore / samples.Count;
            summary.PeakScore = peakScore;
            summary.AverageVelocity = totalVelocity / samples.Count;
            summary.MeanPosition = sum / samples.Count;
            return summary;
        }

        public Texture2D BuildHeatmapTexture()
        {
            if (cachedTexture == null || cachedTexture.width != textureResolution || cachedTexture.height != textureResolution)
            {
                cachedTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false, true);
                cachedTexture.wrapMode = TextureWrapMode.Clamp;
                cachedTexture.filterMode = FilterMode.Bilinear;
            }

            if (!textureDirty)
            {
                return cachedTexture;
            }

            textureDirty = false;
            ClearTexture(cachedTexture);

            if (samples.Count == 0)
            {
                return cachedTexture;
            }

            float[,] heat = new float[textureResolution, textureResolution];
            float radius = Mathf.Max(3f, stampRadius);
            int pixelRadius = Mathf.CeilToInt(radius);
            float radiusSqr = radius * radius;

            for (int i = 0; i < samples.Count; i++)
            {
                Vector2 normalized = NormalizePoint(samples[i].Position);
                int px = Mathf.RoundToInt(normalized.x * (textureResolution - 1));
                int py = Mathf.RoundToInt(normalized.y * (textureResolution - 1));
                float stampIntensity = Mathf.Clamp01(samples[i].Intensity * stampStrength);

                for (int y = -pixelRadius; y <= pixelRadius; y++)
                {
                    int sy = py + y;
                    if (sy < 0 || sy >= textureResolution)
                    {
                        continue;
                    }

                    for (int x = -pixelRadius; x <= pixelRadius; x++)
                    {
                        int sx = px + x;
                        if (sx < 0 || sx >= textureResolution)
                        {
                            continue;
                        }

                        float distSqr = (x * x) + (y * y);
                        if (distSqr > radiusSqr)
                        {
                            continue;
                        }

                        float falloff = 1f - (distSqr / radiusSqr);
                        heat[sx, sy] += falloff * stampIntensity;
                    }
                }
            }

            Color[] pixels = new Color[textureResolution * textureResolution];
            float maxIntensity = 0f;
            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    maxIntensity = Mathf.Max(maxIntensity, heat[x, y]);
                }
            }

            maxIntensity = Mathf.Max(maxIntensity, 0.0001f);

            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    float t = Mathf.Clamp01(heat[x, y] / maxIntensity);
                    pixels[(y * textureResolution) + x] = EvaluateHeatColor(t);
                }
            }

            cachedTexture.SetPixels(pixels);
            cachedTexture.Apply(false, false);
            return cachedTexture;
        }

        private Vector2 NormalizePoint(Vector2 position)
        {
            float nx = Mathf.InverseLerp(playArea.xMin, playArea.xMax, position.x);
            float ny = Mathf.InverseLerp(playArea.yMin, playArea.yMax, position.y);
            return new Vector2(nx, ny);
        }

        private Color EvaluateHeatColor(float t)
        {
            if (t <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            if (t < 0.5f)
            {
                return Color.Lerp(coldColor, midColor, t * 2f);
            }

            return Color.Lerp(midColor, hotColor, (t - 0.5f) * 2f);
        }

        private void ClearTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            Color[] pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0f, 0f, 0f, 0f);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
        }
    }
}
