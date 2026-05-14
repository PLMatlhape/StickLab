using System;
using UnityEngine;

namespace StickLab.Utils
{
    /// <summary>
    /// Lightweight animation tweening system for smooth transitions.
    /// Handles lerping with easing, delays, and callbacks.
    /// </summary>
    public class AnimationTweener
    {
        public enum EasingType
        {
            Linear,
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad,
            EaseInCubic,
            EaseOutCubic,
            EaseInOutCubic,
            EaseOutElastic,
            EaseOutBack
        }

        private Action<float> onUpdate;
        private Action onComplete;
        private float duration;
        private float elapsed;
        private float delay;
        private bool isActive;
        private EasingType easing;
        private bool useUnscaledDeltaTime;

        public bool IsActive => isActive;
        public float Progress => Mathf.Clamp01(elapsed / duration);

        public AnimationTweener(float duration, Action<float> onUpdate, EasingType easingType = EasingType.Linear, float delay = 0f, bool useUnscaled = false)
        {
            this.duration = Mathf.Max(0.001f, duration);
            this.onUpdate = onUpdate;
            this.easing = easingType;
            this.delay = delay;
            this.useUnscaledDeltaTime = useUnscaled;
            this.isActive = true;
            this.elapsed = 0f;
        }

        public void OnComplete(Action callback)
        {
            onComplete = callback;
        }

        public void Update(float deltaTime)
        {
            if (!isActive) return;

            if (delay > 0f)
            {
                delay -= deltaTime;
                if (delay > 0f) return;
                
                deltaTime = -delay; // Use remainder of delta
            }

            elapsed += deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedValue = ApplyEasing(normalizedTime, easing);

            onUpdate?.Invoke(easedValue);

            if (elapsed >= duration)
            {
                isActive = false;
                onComplete?.Invoke();
            }
        }

        public void Stop()
        {
            isActive = false;
        }

        public void Complete()
        {
            elapsed = duration;
            float easedValue = ApplyEasing(1f, easing);
            onUpdate?.Invoke(easedValue);
            isActive = false;
            onComplete?.Invoke();
        }

        private static float ApplyEasing(float t, EasingType type)
        {
            return type switch
            {
                EasingType.Linear => t,
                EasingType.EaseInQuad => t * t,
                EasingType.EaseOutQuad => t * (2f - t),
                EasingType.EaseInOutQuad => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
                EasingType.EaseInCubic => t * t * t,
                EasingType.EaseOutCubic => 1f + (--t) * t * t,
                EasingType.EaseInOutCubic => t < 0.5f ? 4f * t * t * t : 1f + (--t) * 2f * (--t) * (--t) * 2f,
                EasingType.EaseOutElastic => ElasticEaseOut(t),
                EasingType.EaseOutBack => BackEaseOut(t),
                _ => t
            };
        }

        private static float ElasticEaseOut(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            
            float p = 0.3f;
            float s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        private static float BackEaseOut(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }

    /// <summary>
    /// Manager for all active animations - update once per frame.
    /// </summary>
    public class TweenerManager : MonoBehaviour
    {
        private static TweenerManager instance;
        private System.Collections.Generic.List<AnimationTweener> activeTweens = new System.Collections.Generic.List<AnimationTweener>(64);

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            for (int i = activeTweens.Count - 1; i >= 0; i--)
            {
                activeTweens[i].Update(deltaTime);
                
                if (!activeTweens[i].IsActive)
                {
                    activeTweens.RemoveAt(i);
                }
            }
        }

        public static AnimationTweener Create(float duration, Action<float> onUpdate, AnimationTweener.EasingType easingType = AnimationTweener.EasingType.Linear, float delay = 0f)
        {
            if (instance == null)
            {
                GameObject go = new GameObject("TweenerManager");
                instance = go.AddComponent<TweenerManager>();
                DontDestroyOnLoad(go);
            }

            AnimationTweener tween = new AnimationTweener(duration, onUpdate, easingType, delay);
            instance.activeTweens.Add(tween);
            return tween;
        }

        public static void StopAll()
        {
            if (instance != null)
            {
                instance.activeTweens.Clear();
            }
        }
    }
}
