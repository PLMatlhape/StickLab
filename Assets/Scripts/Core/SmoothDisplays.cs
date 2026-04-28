using UnityEngine;
using UnityEngine.UI;
using StickLab.Utils;

namespace StickLab.Core
{
    /// <summary>
    /// Smoothly animates a numeric value from current to target.
    /// Used for score, metrics, etc. to avoid jittery instant updates.
    /// </summary>
    public class SmoothNumberDisplay
    {
        private Text targetText;
        private float currentValue;
        private float targetValue;
        private AnimationTweener activeTween;
        private string formatString;

        public float CurrentValue => currentValue;
        public float TargetValue => targetValue;

        public SmoothNumberDisplay(Text textComponent, string format = "0.0")
        {
            targetText = textComponent;
            formatString = format;
            currentValue = 0f;
            targetValue = 0f;
        }

        public void SetValue(float newValue, float animationDuration = 0.3f)
        {
            if (targetText == null) return;

            targetValue = newValue;

            // Stop existing animation
            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            // Create smooth transition
            float startValue = currentValue;
            activeTween = TweenerManager.Create(
                animationDuration,
                t =>
                {
                    currentValue = Mathf.Lerp(startValue, targetValue, t);
                    if (targetText != null)
                    {
                        targetText.text = currentValue.ToString(formatString);
                    }
                },
                AnimationTweener.EasingType.EaseOutCubic
            );
        }

        public void SetValueInstant(float newValue)
        {
            currentValue = newValue;
            targetValue = newValue;
            
            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            if (targetText != null)
            {
                targetText.text = currentValue.ToString(formatString);
            }
        }

        public void ForceComplete()
        {
            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Complete();
            }
        }
    }

    /// <summary>
    /// Smoothly animates a fill amount (like progress bars or gauges).
    /// </summary>
    public class SmoothFillDisplay
    {
        private Image targetImage;
        private float currentFill;
        private float targetFill;
        private AnimationTweener activeTween;

        public float CurrentFill => currentFill;
        public float TargetFill => targetFill;

        public SmoothFillDisplay(Image imageComponent)
        {
            targetImage = imageComponent;
            currentFill = 0f;
            targetFill = 0f;
        }

        public void SetFill(float newFill, float animationDuration = 0.25f)
        {
            if (targetImage == null) return;

            newFill = Mathf.Clamp01(newFill);
            targetFill = newFill;

            // Stop existing animation
            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            float startFill = currentFill;
            activeTween = TweenerManager.Create(
                animationDuration,
                t =>
                {
                    currentFill = Mathf.Lerp(startFill, targetFill, t);
                    if (targetImage != null)
                    {
                        targetImage.fillAmount = currentFill;
                    }
                },
                AnimationTweener.EasingType.EaseOutCubic
            );
        }

        public void SetFillInstant(float newFill)
        {
            newFill = Mathf.Clamp01(newFill);
            currentFill = newFill;
            targetFill = newFill;

            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            if (targetImage != null)
            {
                targetImage.fillAmount = currentFill;
            }
        }

        public void ForceComplete()
        {
            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Complete();
            }
        }
    }

    /// <summary>
    /// Smoothly animates color transitions.
    /// </summary>
    public class SmoothColorDisplay
    {
        private Graphic targetGraphic;
        private Color currentColor;
        private Color targetColor;
        private AnimationTweener activeTween;

        public Color CurrentColor => currentColor;
        public Color TargetColor => targetColor;

        public SmoothColorDisplay(Graphic graphicComponent)
        {
            targetGraphic = graphicComponent;
            currentColor = Color.white;
            targetColor = Color.white;
        }

        public void SetColor(Color newColor, float animationDuration = 0.2f)
        {
            if (targetGraphic == null) return;

            targetColor = newColor;

            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            Color startColor = targetGraphic.color;
            activeTween = TweenerManager.Create(
                animationDuration,
                t =>
                {
                    currentColor = Color.Lerp(startColor, targetColor, t);
                    if (targetGraphic != null)
                    {
                        targetGraphic.color = currentColor;
                    }
                },
                AnimationTweener.EasingType.EaseOutCubic
            );
        }

        public void SetColorInstant(Color newColor)
        {
            currentColor = newColor;
            targetColor = newColor;

            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            if (targetGraphic != null)
            {
                targetGraphic.color = newColor;
            }
        }

        public void ForceComplete()
        {
            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Complete();
            }
        }
    }
}
