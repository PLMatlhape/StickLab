using UnityEngine;
using UnityEngine.UI;
using StickLab.Utils;

namespace StickLab.Core
{
    /// <summary>
    /// Smoothly animates button state changes (normal, hover, pressed, focused).
    /// Provides COD-style crisp UI feedback with scale and color transitions.
    /// </summary>
    public class AnimatedButton : MonoBehaviour
    {
        [System.Serializable]
        public struct ButtonAnimationSettings
        {
            public float hoverScale = 1.05f;
            public float pressScale = 0.95f;
            public float focusScale = 1.02f;
            public float hoverGlowIntensity = 0.2f;
            public float animationDuration = 0.15f;
            public AnimationTweener.EasingType easing = AnimationTweener.EasingType.EaseOutCubic;
        }

        private Button button;
        private Image buttonImage;
        private Text buttonText;
        private RectTransform rectTransform;
        private ButtonAnimationSettings settings;

        private Color normalColor;
        private Color hoverColor;
        private Color pressColor;
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
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            buttonText = GetComponentInChildren<Text>();
            rectTransform = GetComponent<RectTransform>();
            normalScale = rectTransform.localScale;

            // Default colors
            normalColor = buttonImage != null ? buttonImage.color : Color.white;
            hoverColor = normalColor;
            pressColor = normalColor;
        }

        private void Start()
        {
            // Set up event listeners
            if (button != null)
            {
                var triggers = GetComponent<EventTrigger>();
                if (triggers == null)
                {
                    triggers = gameObject.AddComponent<EventTrigger>();
                }
            }
        }

        public void Initialize(ButtonAnimationSettings animSettings)
        {
            settings = animSettings;
        }

        public void SetColors(Color normal, Color hover, Color press)
        {
            normalColor = normal;
            hoverColor = hover;
            pressColor = press;
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
            
            if (!isFocused)
            {
                AnimateToState(normalScale.x, normalColor);
            }
            else
            {
                AnimateToState(settings.focusScale, hoverColor);
            }
        }

        public void OnPointerDown()
        {
            isPressed = true;
            AnimateToState(settings.pressScale, pressColor);
        }

        public void OnPointerUp()
        {
            isPressed = false;

            if (isHovered)
            {
                AnimateToState(settings.hoverScale, hoverColor);
            }
            else if (isFocused)
            {
                AnimateToState(settings.focusScale, hoverColor);
            }
            else
            {
                AnimateToState(normalScale.x, normalColor);
            }
        }

        public void SetFocused(bool focused)
        {
            isFocused = focused;

            if (focused)
            {
                AnimateToState(settings.focusScale, hoverColor);
                
                // Add glow effect
                if (buttonImage != null)
                {
                    buttonImage.material = new Material(buttonImage.material);
                    // You can enhance this with a glow shader later
                }
            }
            else
            {
                AnimateToState(normalScale.x, normalColor);
                
                if (buttonImage != null)
                {
                    buttonImage.material = new Material(Shader.Find("UI/Default"));
                }
            }
        }

        public void AnimatePress()
        {
            // Spring-like press animation
            TweenerManager.Create(
                0.1f,
                t => rectTransform.localScale = Vector3.Lerp(normalScale * settings.pressScale, normalScale, t),
                AnimationTweener.EasingType.EaseOutElastic
            );
        }

        private void AnimateToState(float targetScale, Color targetColor)
        {
            // Stop existing animations
            if (scaleAnimation != null && scaleAnimation.IsActive)
            {
                scaleAnimation.Stop();
            }
            if (colorAnimation != null && colorAnimation.IsActive)
            {
                colorAnimation.Stop();
            }

            // Animate scale
            Vector3 startScale = rectTransform.localScale;
            Vector3 targetScaleVector = new Vector3(targetScale, targetScale, 1f);
            scaleAnimation = TweenerManager.Create(
                settings.animationDuration,
                t =>
                {
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = Vector3.Lerp(startScale, targetScaleVector, t);
                    }
                },
                settings.easing
            );

            // Animate color
            if (buttonImage != null)
            {
                Color startColor = buttonImage.color;
                colorAnimation = TweenerManager.Create(
                    settings.animationDuration,
                    t =>
                    {
                        if (buttonImage != null)
                        {
                            buttonImage.color = Color.Lerp(startColor, targetColor, t);
                        }
                    },
                    settings.easing
                );
            }
        }

        private void OnDestroy()
        {
            if (scaleAnimation != null)
            {
                scaleAnimation.Stop();
            }
            if (colorAnimation != null)
            {
                colorAnimation.Stop();
            }
        }
    }

    /// <summary>
    /// Smooth fade in/out for overlay panels and UI elements.
    /// </summary>
    public class SmoothPanelTransition
    {
        private CanvasGroup canvasGroup;
        private AnimationTweener activeTween;

        public SmoothPanelTransition(CanvasGroup group)
        {
            canvasGroup = group;
        }

        public void FadeIn(float duration = 0.3f)
        {
            if (canvasGroup == null) return;

            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            activeTween = TweenerManager.Create(
                duration,
                t =>
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = t;
                    }
                },
                AnimationTweener.EasingType.EaseOutCubic
            );

            activeTween.OnComplete(() =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            });
        }

        public void FadeOut(float duration = 0.3f)
        {
            if (canvasGroup == null) return;

            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            canvasGroup.interactable = false;
            float startAlpha = canvasGroup.alpha;

            activeTween = TweenerManager.Create(
                duration,
                t =>
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                    }
                },
                AnimationTweener.EasingType.EaseOutCubic
            );

            activeTween.OnComplete(() =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = false;
                }
            });
        }

        public void SetAlphaInstant(float alpha)
        {
            if (activeTween != null && activeTween.IsActive)
            {
                activeTween.Stop();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                canvasGroup.interactable = alpha > 0.5f;
                canvasGroup.blocksRaycasts = alpha > 0.5f;
            }
        }
    }
}
