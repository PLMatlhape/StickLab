using System.Collections.Generic;
using UnityEngine;
using StickLab.Core;

namespace StickLab.Analytics
{
    public class HeatmapReplayController : MonoBehaviour
    {
        [Header("Replay")]
        [SerializeField] private HeatmapRecorder sourceRecorder;
        [SerializeField] private Transform replayCursor;
        [SerializeField] private LineRenderer replayPathRenderer;
        [SerializeField, Min(0.1f)] private float replaySpeed = 1f;
        [SerializeField] private bool loopReplay;

        public bool IsPlaying { get; private set; }
        public float PlaybackTime { get; private set; }
        public float PlaybackDuration => cachedSamples.Count > 0 ? cachedSamples[cachedSamples.Count - 1].Timestamp : 0f;
        public int CurrentSampleIndex { get; private set; }

        private readonly List<HeatmapSample> cachedSamples = new List<HeatmapSample>(4096);
        private readonly List<Vector3> pathPoints = new List<Vector3>(4096);

        private void Awake()
        {
            if (replayPathRenderer != null)
            {
                replayPathRenderer.positionCount = 0;
            }

            SetCursorVisible(false);
        }

        private void Update()
        {
            if (!IsPlaying || cachedSamples.Count == 0)
            {
                return;
            }

            PlaybackTime += Time.deltaTime * Mathf.Max(0.1f, replaySpeed);
            AdvanceReplay();
        }

        public void SetSource(HeatmapRecorder recorder)
        {
            sourceRecorder = recorder;
        }

        public void SetReplayCursor(Transform cursor)
        {
            replayCursor = cursor;
            SetCursorVisible(false);
        }

        public void SetReplayPathRenderer(LineRenderer pathRenderer)
        {
            replayPathRenderer = pathRenderer;
            if (replayPathRenderer != null)
            {
                replayPathRenderer.positionCount = 0;
            }
        }

        public void LoadFromRecorder()
        {
            cachedSamples.Clear();
            pathPoints.Clear();

            if (sourceRecorder == null)
            {
                return;
            }

            IReadOnlyList<HeatmapSample> source = sourceRecorder.Samples;
            for (int i = 0; i < source.Count; i++)
            {
                cachedSamples.Add(source[i]);
                pathPoints.Add(new Vector3(source[i].Position.x, source[i].Position.y, 0f));
            }

            if (replayPathRenderer != null)
            {
                replayPathRenderer.positionCount = 0;
            }
        }

        public void Play()
        {
            if (cachedSamples.Count == 0)
            {
                LoadFromRecorder();
            }

            if (cachedSamples.Count == 0)
            {
                return;
            }

            PlaybackTime = 0f;
            CurrentSampleIndex = 0;
            IsPlaying = true;
            SetCursorVisible(true);
            AdvanceReplay();
        }

        public void Stop()
        {
            IsPlaying = false;
            PlaybackTime = 0f;
            CurrentSampleIndex = 0;
            SetCursorVisible(false);

            if (replayPathRenderer != null)
            {
                replayPathRenderer.positionCount = 0;
            }
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public string GetStatusText()
        {
            if (cachedSamples.Count == 0)
            {
                return "No replay data";
            }

            if (!IsPlaying)
            {
                return $"Replay ready • {cachedSamples.Count} samples";
            }

            return $"Replaying • {PlaybackTime:0.0}s / {PlaybackDuration:0.0}s";
        }

        private void AdvanceReplay()
        {
            if (cachedSamples.Count == 0)
            {
                return;
            }

            while (CurrentSampleIndex < cachedSamples.Count - 1 && cachedSamples[CurrentSampleIndex + 1].Timestamp <= PlaybackTime)
            {
                CurrentSampleIndex++;
            }

            HeatmapSample current = cachedSamples[Mathf.Clamp(CurrentSampleIndex, 0, cachedSamples.Count - 1)];
            HeatmapSample next = cachedSamples[Mathf.Clamp(CurrentSampleIndex + 1, 0, cachedSamples.Count - 1)];

            float segmentDuration = Mathf.Max(next.Timestamp - current.Timestamp, 0.0001f);
            float lerpT = Mathf.Clamp01((PlaybackTime - current.Timestamp) / segmentDuration);
            Vector2 position = Vector2.Lerp(current.Position, next.Position, lerpT);
            SetReplayCursor(position);
            UpdateReplayPath();

            if (PlaybackTime >= PlaybackDuration)
            {
                if (loopReplay)
                {
                    PlaybackTime = 0f;
                    CurrentSampleIndex = 0;
                    UpdateReplayPath();
                }
                else
                {
                    IsPlaying = false;
                    SetCursorVisible(false);
                }
            }
        }

        private void SetReplayCursor(Vector2 position)
        {
            if (replayCursor != null)
            {
                replayCursor.position = new Vector3(position.x, position.y, replayCursor.position.z);
            }
        }

        private void UpdateReplayPath()
        {
            if (replayPathRenderer == null)
            {
                return;
            }

            int count = Mathf.Clamp(CurrentSampleIndex + 1, 0, pathPoints.Count);
            replayPathRenderer.positionCount = count;

            for (int i = 0; i < count; i++)
            {
                replayPathRenderer.SetPosition(i, pathPoints[i]);
            }
        }

        private void SetCursorVisible(bool visible)
        {
            if (replayCursor != null)
            {
                replayCursor.gameObject.SetActive(visible);
            }
        }
    }
}
