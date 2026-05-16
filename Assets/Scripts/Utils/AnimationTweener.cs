using System;
using System.Collections.Generic;
using UnityEngine;

namespace StickLab.Utils
{
    public class AnimationTweener
    {
        public enum EasingType
        {
            Linear,
            EaseInQuad, EaseOutQuad, EaseInOutQuad,
            EaseInCubic, EaseOutCubic, EaseInOutCubic,
            EaseOutElastic, EaseOutBack
        }

        private Action<float> onUpdate;
        private Action        onComplete;
        private float         duration;
        private float         elapsed;
        private float         delay;
        private EasingType    easing;
        private bool          isActive;

        public bool  IsActive  => isActive;
        public float Progress  => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        public AnimationTweener(float duration, Action<float> onUpdate,
            EasingType easingType = EasingType.Linear, float delay = 0f)
        {
            this.duration  = Mathf.Max(0.001f, duration);
            this.onUpdate  = onUpdate;
            this.easing    = easingType;
            this.delay     = delay;
            this.isActive  = true;
            this.elapsed   = 0f;
        }

        public void OnComplete(Action cb) => onComplete = cb;

        public void Update(float dt)
        {
            if (!isActive) return;

            if (delay > 0f)
            {
                delay -= dt;
                if (delay > 0f) return;
                dt = -delay; // carry remainder
            }

            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);
            onUpdate?.Invoke(Ease(t, easing));

            if (elapsed >= duration)
            {
                isActive = false;
                onComplete?.Invoke();
            }
        }

        public void Stop()     => isActive = false;

        public void Complete()
        {
            elapsed  = duration;
            onUpdate?.Invoke(Ease(1f, easing));
            isActive = false;
            onComplete?.Invoke();
        }

        // FIX: EaseInOutCubic previously used (--t) three times in one expression,
        // mutating t with undefined evaluation order in C#. Now uses a local copy.
        private static float Ease(float t, EasingType type)
        {
            switch (type)
            {
                case EasingType.Linear:        return t;
                case EasingType.EaseInQuad:    return t * t;
                case EasingType.EaseOutQuad:   return t * (2f - t);
                case EasingType.EaseInOutQuad: return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
                case EasingType.EaseInCubic:   return t * t * t;
                case EasingType.EaseOutCubic:
                {
                    float u = t - 1f;
                    return 1f + u * u * u;
                }
                case EasingType.EaseInOutCubic:
                {
                    // FIX: was `1f + (--t) * 2f * (--t) * (--t) * 2f` — mutated t 3x
                    if (t < 0.5f) return 4f * t * t * t;
                    float u = t - 1f;
                    return 1f + 2f * u * u * u; // correct: 2(t-1)^3 + 1
                }
                case EasingType.EaseOutElastic: return EaseOutElastic(t);
                case EasingType.EaseOutBack:    return EaseOutBack(t);
                default:                         return t;
            }
        }

        private static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float p = 0.3f, s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * Mathf.PI * 2f / p) + 1f;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }

    public class TweenerManager : MonoBehaviour
    {
        private static TweenerManager instance;
        private readonly List<AnimationTweener> active = new List<AnimationTweener>(64);

        private void Awake()
        {
            if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
            else if (instance != this) Destroy(gameObject);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].Update(dt);
                if (!active[i].IsActive) active.RemoveAt(i);
            }
        }

        public static AnimationTweener Create(float duration, Action<float> onUpdate,
            AnimationTweener.EasingType easing = AnimationTweener.EasingType.Linear, float delay = 0f)
        {
            if (instance == null)
            {
                var go = new GameObject("TweenerManager");
                instance = go.AddComponent<TweenerManager>();
                DontDestroyOnLoad(go);
            }
            var t = new AnimationTweener(duration, onUpdate, easing, delay);
            instance.active.Add(t);
            return t;
        }

        public static void StopAll() => instance?.active.Clear();
    }
}
