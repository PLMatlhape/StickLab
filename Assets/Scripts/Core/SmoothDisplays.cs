using UnityEngine;
using UnityEngine.UI;
using StickLab.Utils;

namespace StickLab.Core
{
    /// <summary>Smoothly animates a numeric Text value.</summary>
    public class SmoothNumberDisplay
    {
        private Text           targetText;
        private float          currentValue;
        private float          targetValue;
        private AnimationTweener activeTween;
        private string         formatString;

        public float CurrentValue => currentValue;
        public float TargetValue  => targetValue;

        public SmoothNumberDisplay(Text text, string format = "0.0")
        {
            targetText   = text;
            formatString = format;
        }

        public void SetValue(float newValue, float duration = 0.3f)
        {
            if (targetText == null) return;
            targetValue = newValue;
            activeTween?.Stop();
            float start = currentValue;
            activeTween = TweenerManager.Create(duration, t =>
            {
                currentValue = Mathf.Lerp(start, targetValue, t);
                if (targetText != null) targetText.text = currentValue.ToString(formatString);
            }, AnimationTweener.EasingType.EaseOutCubic);
        }

        public void SetValueInstant(float newValue)
        {
            activeTween?.Stop();
            currentValue = targetValue = newValue;
            if (targetText != null) targetText.text = currentValue.ToString(formatString);
        }

        public void ForceComplete() => activeTween?.Complete();
    }

    /// <summary>Smoothly animates an Image fill amount.</summary>
    public class SmoothFillDisplay
    {
        private Image          targetImage;
        private float          currentFill;
        private float          targetFill;
        private AnimationTweener activeTween;

        public float CurrentFill => currentFill;
        public float TargetFill  => targetFill;

        public SmoothFillDisplay(Image image)
        {
            targetImage = image;
        }

        public void SetFill(float newFill, float duration = 0.25f)
        {
            if (targetImage == null) return;
            newFill = Mathf.Clamp01(newFill);
            targetFill = newFill;
            activeTween?.Stop();
            float start = currentFill;
            activeTween = TweenerManager.Create(duration, t =>
            {
                currentFill = Mathf.Lerp(start, targetFill, t);
                if (targetImage != null) targetImage.fillAmount = currentFill;
            }, AnimationTweener.EasingType.EaseOutCubic);
        }

        public void SetFillInstant(float newFill)
        {
            activeTween?.Stop();
            currentFill = targetFill = Mathf.Clamp01(newFill);
            if (targetImage != null) targetImage.fillAmount = currentFill;
        }

        public void ForceComplete() => activeTween?.Complete();
    }

    /// <summary>Smoothly animates a Graphic color.</summary>
    public class SmoothColorDisplay
    {
        private Graphic        targetGraphic;
        private Color          currentColor;
        private Color          targetColor;
        private AnimationTweener activeTween;

        public Color CurrentColor => currentColor;

        public SmoothColorDisplay(Graphic graphic)
        {
            targetGraphic = graphic;
            currentColor  = graphic != null ? graphic.color : Color.white;
            targetColor   = currentColor;
        }

        public void SetColor(Color newColor, float duration = 0.2f)
        {
            if (targetGraphic == null) return;
            targetColor = newColor;
            activeTween?.Stop();
            Color start = currentColor;
            activeTween = TweenerManager.Create(duration, t =>
            {
                currentColor = Color.Lerp(start, targetColor, t);
                if (targetGraphic != null) targetGraphic.color = currentColor;
            }, AnimationTweener.EasingType.EaseOutCubic);
        }

        public void SetColorInstant(Color newColor)
        {
            activeTween?.Stop();
            currentColor = targetColor = newColor;
            if (targetGraphic != null) targetGraphic.color = newColor;
        }

        public void ForceComplete() => activeTween?.Complete();
    }

    /// <summary>
    /// Fade a CanvasGroup in/out.
    /// FIX: tracks visible state so FadeIn/FadeOut called every frame
    /// does NOT restart the tween on every call — only transitions on state change.
    /// </summary>
    public class SmoothPanelTransition
    {
        private CanvasGroup      canvasGroup;
        private AnimationTweener activeTween;
        private bool             isVisible;      // FIX: state guard
        private bool             isFading;       // FIX: in-progress guard

        public bool IsVisible => isVisible;

        public SmoothPanelTransition(CanvasGroup group)
        {
            canvasGroup = group;
        }

        /// <summary>Only starts a new fade-in if not already visible or fading in.</summary>
        public void FadeIn(float duration = 0.3f)
        {
            if (canvasGroup == null) return;
            // FIX: skip if already fully visible and not currently fading out
            if (isVisible && !isFading) return;

            isVisible = true;
            isFading  = true;
            activeTween?.Stop();

            canvasGroup.blocksRaycasts = true;
            float start = canvasGroup.alpha;

            activeTween = TweenerManager.Create(duration, t =>
            {
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(start, 1f, t);
            }, AnimationTweener.EasingType.EaseOutCubic);

            activeTween.OnComplete(() =>
            {
                isFading = false;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha        = 1f;
                    canvasGroup.interactable = true;
                }
            });
        }

        /// <summary>Only starts a new fade-out if not already hidden or fading out.</summary>
        public void FadeOut(float duration = 0.25f)
        {
            if (canvasGroup == null) return;
            // FIX: skip if already hidden and not currently fading in
            if (!isVisible && !isFading) return;

            isVisible = false;
            isFading  = true;
            activeTween?.Stop();

            canvasGroup.interactable = false;
            float start = canvasGroup.alpha;

            activeTween = TweenerManager.Create(duration, t =>
            {
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(start, 0f, t);
            }, AnimationTweener.EasingType.EaseOutCubic);

            activeTween.OnComplete(() =>
            {
                isFading = false;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha           = 0f;
                    canvasGroup.blocksRaycasts  = false;
                }
            });
        }

        public void SetAlphaInstant(float alpha)
        {
            activeTween?.Stop();
            isFading  = false;
            isVisible = alpha > 0.5f;
            if (canvasGroup == null) return;
            canvasGroup.alpha           = alpha;
            canvasGroup.interactable    = isVisible;
            canvasGroup.blocksRaycasts  = isVisible;
        }
    }
}
