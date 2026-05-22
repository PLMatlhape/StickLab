using UnityEngine;
using UnityEngine.UI;
using StickLab.Utils;

namespace StickLab.Core
{
    /// <summary>
    /// Smoothly animates button state changes (normal, hover, pressed, focused).
    /// </summary>
    public class AnimatedButton : MonoBehaviour
    {
        // FIX: C#9 structs cannot have inline field defaults.
        // Use an explicit constructor with default parameters instead.
        [System.Serializable]
        public struct ButtonAnimationSettings
        {
            public float hoverScale;
            public float pressScale;
            public float focusScale;
            public float hoverGlowIntensity;
            public float animationDuration;
            public AnimationTweener.EasingType easing;

            // FIX: explicit constructor replaces inline defaults — works on C# 9 (Unity 2022.3)
            public ButtonAnimationSettings(
                float hoverScale         = 1.05f,
                float pressScale         = 0.95f,
                float focusScale         = 1.02f,
                float hoverGlowIntensity = 0.2f,
                float animationDuration  = 0.15f,
                AnimationTweener.EasingType easing = AnimationTweener.EasingType.EaseOutCubic)
            {
                this.hoverScale         = hoverScale;
                this.pressScale         = pressScale;
                this.focusScale         = focusScale;
                this.hoverGlowIntensity = hoverGlowIntensity;
                this.animationDuration  = animationDuration;
                this.easing             = easing;
            }

            // Sensible defaults accessible without constructing
            public static ButtonAnimationSettings Default => new ButtonAnimationSettings();
        }

        private Button           button;
        private Image            buttonImage;
        private Text             buttonText;
        private RectTransform    rectTransform;
        private ButtonAnimationSettings settings;

        private Color normalColor, hoverColor, pressColor;
        private Vector3 normalScale = Vector3.one;

        private AnimationTweener scaleAnimation;
        private AnimationTweener colorAnimation;

        private bool isHovered;
        private bool isPressed;
        private bool isFocused;

        public bool IsHovered => isHovered;
        public bool IsPressed => isPressed;
        public bool IsFocused => isFocused;

        private void Awake()
        {
            button        = GetComponent<Button>();
            buttonImage   = GetComponent<Image>();
            buttonText    = GetComponentInChildren<Text>();
            rectTransform = GetComponent<RectTransform>();
            normalScale   = rectTransform.localScale;
            normalColor   = buttonImage != null ? buttonImage.color : Color.white;
            hoverColor    = normalColor;
            pressColor    = normalColor;
            settings      = ButtonAnimationSettings.Default;
        }

        public void Initialize(ButtonAnimationSettings s) => settings = s;

        public void SetColors(Color normal, Color hover, Color press)
        {
            normalColor = normal;
            hoverColor  = hover;
            pressColor  = press;
        }

        public void OnPointerEnter()
        {
            if (isPressed) return;
            isHovered = true;
            AnimateToState(settings.hoverScale, hoverColor);
        }

        public void OnPointerExit()
        {
            isHovered = false;
            AnimateToState(isFocused ? settings.focusScale : normalScale.x,
                           isFocused ? hoverColor : normalColor);
        }

        public void OnPointerDown()
        {
            isPressed = true;
            AnimateToState(settings.pressScale, pressColor);
        }

        public void OnPointerUp()
        {
            isPressed = false;
            if      (isHovered) AnimateToState(settings.hoverScale, hoverColor);
            else if (isFocused) AnimateToState(settings.focusScale, hoverColor);
            else                AnimateToState(normalScale.x, normalColor);
        }

        public void SetFocused(bool focused)
        {
            isFocused = focused;
            AnimateToState(focused ? settings.focusScale : normalScale.x,
                           focused ? hoverColor : normalColor);
        }

        public void AnimatePress()
        {
            TweenerManager.Create(0.1f,
                t => rectTransform.localScale = Vector3.Lerp(normalScale * settings.pressScale, normalScale, t),
                AnimationTweener.EasingType.EaseOutElastic);
        }

        private void AnimateToState(float targetScale, Color targetColor)
        {
            scaleAnimation?.Stop();
            colorAnimation?.Stop();

            Vector3 startScale  = rectTransform.localScale;
            Vector3 targetScaleV = new Vector3(targetScale, targetScale, 1f);

            scaleAnimation = TweenerManager.Create(settings.animationDuration,
                t => { if (rectTransform != null) rectTransform.localScale = Vector3.Lerp(startScale, targetScaleV, t); },
                settings.easing);

            if (buttonImage != null)
            {
                Color startColor = buttonImage.color;
                colorAnimation = TweenerManager.Create(settings.animationDuration,
                    t => { if (buttonImage != null) buttonImage.color = Color.Lerp(startColor, targetColor, t); },
                    settings.easing);
            }
        }

        private void OnDestroy()
        {
            scaleAnimation?.Stop();
            colorAnimation?.Stop();
        }
    }

    // NOTE: SmoothPanelTransition has been moved to SmoothDisplays.cs.
    // It is no longer defined here to avoid CS0101 duplicate definition.
    // If you had references to SmoothPanelTransition from AnimatedButton.cs,
    // they will continue to work — same namespace, same class name.
}
